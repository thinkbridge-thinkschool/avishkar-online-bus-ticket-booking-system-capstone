using BusBooking.Application.Buses;
using BusBooking.Application.Cities;
using BusBooking.Application.Vendors;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Commands.CreateSchedule;
using BusBooking.Application.Scheduling.Commands.DeleteSchedule;
using BusBooking.Application.Scheduling.Commands.UpdateSchedule;
using BusBooking.Application.Scheduling.Queries.GetScheduleById;
using BusBooking.Application.Scheduling.Queries.GetVendorSchedules;
using BusBooking.Application.Scheduling.Queries.SearchSchedules;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Api.Scheduling;

public static class ScheduleEndpoints
{
    public static void MapScheduleEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/schedules")
            .WithTags("Schedules")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/search", SearchSchedules)
            .AllowAnonymous()
            .CacheOutput(p => p
                .Expire(TimeSpan.FromMinutes(2))
                .SetVaryByQuery("fromCityId", "toCityId", "travelDate"));
        group.MapGet("/{scheduleId:guid}", GetScheduleById).AllowAnonymous();
        group.MapGet("/{scheduleId:guid}/seats", GetSeats).AllowAnonymous();
        group.MapGet("/mine", GetMySchedules);
        group.MapGet("/vendor/{vendorId:guid}", GetVendorSchedules);
        group.MapPost("/", CreateSchedule);
        group.MapPut("/{scheduleId:guid}", UpdateSchedule);
        group.MapDelete("/{scheduleId:guid}", DeleteSchedule);
    }

    private static async Task<IResult> SearchSchedules(
        Guid fromCityId, Guid toCityId, DateOnly travelDate,
        IScheduleRepository scheduleRepo, ICityRepository cityRepo, CancellationToken ct)
    {
        var fromCity = await cityRepo.GetByIdAsync(fromCityId, ct);
        var toCity   = await cityRepo.GetByIdAsync(toCityId, ct);
        if (fromCity is null || toCity is null)
            return Results.NotFound("One or more cities not found.");

        var handler = new SearchSchedulesHandler(scheduleRepo);
        var results = await handler.HandleAsync(
            new SearchSchedulesQuery(fromCity.CityName, toCity.CityName, travelDate), ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> GetScheduleById(
        Guid scheduleId, IScheduleRepository scheduleRepo, CancellationToken ct)
    {
        var handler = new GetScheduleByIdHandler(scheduleRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetScheduleByIdQuery(scheduleId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }

    private static async Task<IResult> GetSeats(
        Guid scheduleId, IScheduleRepository scheduleRepo, CancellationToken ct)
    {
        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(scheduleId, ct);
        if (schedule is null) return Results.NotFound();

        var seats = schedule.Seats.Select(s => new
        {
            s.SeatNumber,
            SeatType = s.SeatType.ToString(),
            Status = s.Status.ToString(),
            s.Price,
        });

        return Results.Ok(seats);
    }

    private static async Task<IResult> GetVendorSchedules(
        Guid vendorId, IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var handler = new GetVendorSchedulesHandler(scheduleRepo, busRepo);
        var results = await handler.HandleAsync(new GetVendorSchedulesQuery(vendorId), ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> CreateSchedule(
        CreateScheduleCommand command,
        IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var handler = new CreateScheduleHandler(busRepo, scheduleRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/schedules/{id}", new { scheduleId = id });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    }

    private static async Task<IResult> UpdateSchedule(
        Guid scheduleId, UpdateScheduleRequest body,
        IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var command = new UpdateScheduleCommand(scheduleId, body.RequestingVendorId, body.DepartureTime, body.ArrivalTime);
        var handler = new UpdateScheduleHandler(scheduleRepo, busRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeleteSchedule(
        Guid scheduleId, Guid requestingVendorId,
        IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var handler = new DeleteScheduleHandler(scheduleRepo, busRepo);
        try
        {
            await handler.HandleAsync(new DeleteScheduleCommand(scheduleId, requestingVendorId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetMySchedules(
        HttpContext httpContext, IVendorRepository vendorRepo,
        IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor not found.");

        var handler = new GetVendorSchedulesHandler(scheduleRepo, busRepo);
        var results = await handler.HandleAsync(new GetVendorSchedulesQuery(vendor.Id), ct);
        return Results.Ok(results);
    }

    private static string? GetEntraOid(HttpContext ctx) =>
        ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? ctx.User.FindFirst("oid")?.Value;
}

public sealed record UpdateScheduleRequest(
    Guid RequestingVendorId, TimeOnly DepartureTime, TimeOnly ArrivalTime);

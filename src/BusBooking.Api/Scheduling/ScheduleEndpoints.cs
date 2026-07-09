using BusBooking.Application.Buses;
using BusBooking.Application.Cities;
using BusBooking.Application.Routes;
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

    private static async Task<IResult> SearchSchedules(     // Searches for schedules between two cities on a given travel date.
        Guid fromCityId, Guid toCityId, DateOnly travelDate,
        IScheduleRepository scheduleRepo, ICityRepository cityRepo, CancellationToken ct)
    {
        var fromCity = await cityRepo.GetByIdAsync(fromCityId, ct);
        var toCity   = await cityRepo.GetByIdAsync(toCityId, ct);
        if (fromCity is null || toCity is null)
            return Results.NotFound("One or more cities not found.");

        var handler = new SearchSchedulesHandler(scheduleRepo);
        var results = await handler.HandleAsync(
            new SearchSchedulesQuery(fromCity.CityName, toCity.CityName, travelDate), ct); // request query is passed to the handler to get the results from the database.
        return Results.Ok(results);
    }

    private static async Task<IResult> GetScheduleById(     // Returns details for the specified schedule.
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

    private static async Task<IResult> GetSeats(     // Returns the seat map and pricing for the specified schedule.
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

    private static async Task<IResult> GetVendorSchedules(     // Returns all schedules belonging to the specified vendor.
        Guid vendorId, IScheduleRepository scheduleRepo, IBusRepository busRepo, IRouteRepository routeRepo, CancellationToken ct)
    {
        var handler = new GetVendorSchedulesHandler(scheduleRepo, busRepo, routeRepo);
        var results = await handler.HandleAsync(new GetVendorSchedulesQuery(vendorId), ct);
        return Results.Ok(results);
    }

    private static async Task<IResult> CreateSchedule(     // Creates a new schedule for a bus owned by the authenticated vendor.
        CreateScheduleBody body, HttpContext httpContext,
        IVendorRepository vendorRepo, IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found. Register as a vendor first.");

        var command = new CreateScheduleCommand(
            body.BusId, body.RouteId, body.TravelDate, body.DepartureTime, body.ArrivalTime, body.BasePrice, vendor.Id);
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

    private static async Task<IResult> UpdateSchedule(     // Updates the departure and arrival times of a schedule owned by the authenticated vendor.
        Guid scheduleId, UpdateScheduleBody body, HttpContext httpContext,
        IVendorRepository vendorRepo, IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found.");

        var command = new UpdateScheduleCommand(scheduleId, vendor.Id, body.DepartureTime, body.ArrivalTime);
        var handler = new UpdateScheduleHandler(scheduleRepo, busRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeleteSchedule(     // Removes a schedule owned by the authenticated vendor.
        Guid scheduleId, HttpContext httpContext,
        IVendorRepository vendorRepo, IScheduleRepository scheduleRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found.");

        var handler = new DeleteScheduleHandler(scheduleRepo, busRepo);
        try
        {
            await handler.HandleAsync(new DeleteScheduleCommand(scheduleId, vendor.Id), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetMySchedules(     // Returns all schedules belonging to the authenticated vendor.
        HttpContext httpContext, IVendorRepository vendorRepo,
        IScheduleRepository scheduleRepo, IBusRepository busRepo, IRouteRepository routeRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor not found.");

        var handler = new GetVendorSchedulesHandler(scheduleRepo, busRepo, routeRepo);
        var results = await handler.HandleAsync(new GetVendorSchedulesQuery(vendor.Id), ct);
        return Results.Ok(results);
    }

    private static string? GetAppUserId(HttpContext ctx) =>
        ctx.User.FindFirst("app:userId")?.Value;
}

public sealed record CreateScheduleBody(
    Guid BusId, Guid RouteId, DateOnly TravelDate, TimeOnly DepartureTime, TimeOnly ArrivalTime, decimal BasePrice);
public sealed record UpdateScheduleBody(TimeOnly DepartureTime, TimeOnly ArrivalTime);

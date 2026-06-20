using BusBooking.Application.Buses;
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

        group.MapGet("/search", SearchSchedules);
        group.MapGet("/{scheduleId:guid}", GetScheduleById);
        group.MapGet("/{scheduleId:guid}/seats", GetSeats);
        group.MapGet("/vendor/{vendorId:guid}", GetVendorSchedules);
        group.MapPost("/", CreateSchedule);
        group.MapPut("/{scheduleId:guid}", UpdateSchedule);
        group.MapDelete("/{scheduleId:guid}", DeleteSchedule);
    }

    private static async Task<IResult> SearchSchedules(
        string source, string destination, DateOnly travelDate,
        IScheduleRepository scheduleRepo, CancellationToken ct)
    {
        if (source.Length > 100)
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["source"] = ["Max 100 characters."] });
        if (destination.Length > 100)
            return Results.ValidationProblem(
                new Dictionary<string, string[]> { ["destination"] = ["Max 100 characters."] });

        var handler = new SearchSchedulesHandler(scheduleRepo);
        var results = await handler.HandleAsync(new SearchSchedulesQuery(source, destination, travelDate), ct);
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
}

public sealed record UpdateScheduleRequest(
    Guid RequestingVendorId, TimeOnly DepartureTime, TimeOnly ArrivalTime);

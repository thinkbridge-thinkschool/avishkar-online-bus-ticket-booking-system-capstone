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
        group.MapGet("/{scheduleId:guid}/seats", GetSeats);
    }

    private static async Task<IResult> SearchSchedules(
        string source,
        string destination,
        DateOnly travelDate,
        IScheduleRepository scheduleRepo,
        CancellationToken ct)
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

    private static async Task<IResult> GetSeats(
        Guid scheduleId,
        IScheduleRepository scheduleRepo,
        CancellationToken ct)
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
}

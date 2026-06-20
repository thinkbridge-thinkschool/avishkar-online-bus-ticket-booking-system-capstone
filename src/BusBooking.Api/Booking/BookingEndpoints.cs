using System.Security.Claims;
using BusBooking.Application.Booking.Commands.CancelBooking;
using BusBooking.Application.Booking.Commands.CreateBooking;
using BusBooking.Application.Booking.Queries.GetUserBookings;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Api.Booking;

public static class BookingEndpoints
{
    public static void MapBookingEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/bookings")
            .WithTags("Bookings")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/", CreateBooking);
        group.MapGet("/user/{userId:guid}", GetUserBookings);
        group.MapPost("/{bookingId:guid}/cancel", CancelBooking);
    }

    private static async Task<IResult> CreateBooking(
        CreateBookingCommand command,
        IScheduleRepository scheduleRepo,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var handler = new CreateBookingHandler(scheduleRepo, bookingRepo);
        try
        {
            var bookingId = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/bookings/{bookingId}", new { bookingId });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetUserBookings(
        Guid userId,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var handler = new GetUserBookingsHandler(bookingRepo);
        var dtos = await handler.HandleAsync(new GetUserBookingsQuery(userId), ct);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> CancelBooking(
        Guid bookingId,
        HttpContext httpContext,
        IBookingRepository bookingRepo,
        IScheduleRepository scheduleRepo,
        IEventPublisher publisher,
        CancellationToken ct)
    {
        // Extract the caller's identity from the validated JWT — never from the
        // request body, which the caller could forge to act as another user.
        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var handler = new CancelBookingHandler(bookingRepo, scheduleRepo, publisher);
        try
        {
            await handler.HandleAsync(new CancelBookingCommand(bookingId, userId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }
}

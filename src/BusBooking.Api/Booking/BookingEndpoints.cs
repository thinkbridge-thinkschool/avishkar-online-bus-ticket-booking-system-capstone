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
        group.MapGet("/my", GetMyBookings);
        group.MapGet("/{bookingId:guid}", GetBookingById);
        group.MapGet("/user/{userId:guid}", GetUserBookings);
        group.MapPost("/{bookingId:guid}/cancel", CancelBooking);
    }

    private static async Task<IResult> CreateBooking(
        CreateBookingBody body,
        ClaimsPrincipal principal,
        IScheduleRepository scheduleRepo,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var oidValue = principal.FindFirst("oid")?.Value
                    ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (oidValue is null || !Guid.TryParse(oidValue, out var userId))
            return Results.Unauthorized();

        var userEmail = principal.FindFirst("preferred_username")?.Value
                     ?? principal.FindFirst("upn")?.Value
                     ?? principal.FindFirst(ClaimTypes.Email)?.Value
                     ?? "";
        var userName = principal.FindFirst("name")?.Value
                    ?? principal.FindFirst(ClaimTypes.Name)?.Value
                    ?? userEmail;

        var command = new CreateBookingCommand(userId, userEmail, userName, body.ScheduleId, body.Seats);
        var handler = new CreateBookingHandler(scheduleRepo, bookingRepo);
        try
        {
            var bookingId = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/bookings/{bookingId}", new { bookingId });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
        catch (ArgumentException ex) { return Results.BadRequest(ex.Message); }
    }

    private static async Task<IResult> GetMyBookings(
        ClaimsPrincipal principal,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var oidValue = principal.FindFirst("oid")?.Value
                    ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        if (!Guid.TryParse(oidValue, out var userId))
            return Results.Unauthorized();

        var handler = new GetUserBookingsHandler(bookingRepo);
        var dtos = await handler.HandleAsync(new GetUserBookingsQuery(userId), ct);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetBookingById(
        Guid bookingId,
        ClaimsPrincipal principal,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var oidValue = principal.FindFirst("oid")?.Value
                    ?? principal.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
        if (!Guid.TryParse(oidValue, out var userId))
            return Results.Unauthorized();

        var booking = await bookingRepo.GetByIdAsync(bookingId, ct);
        if (booking is null) return Results.NotFound();
        if (booking.UserId != userId) return Results.Forbid();

        var dto = new BookingDto(
            booking.Id,
            booking.ScheduleId,
            booking.Status,
            booking.TotalAmount,
            booking.BookedAt,
            booking.Seats
                .Select(s => new BookedSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.SeatPrice, s.PassengerGender))
                .ToList());

        return Results.Ok(dto);
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
        var userIdClaim = httpContext.User.FindFirst("oid")?.Value
                       ?? httpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                       ?? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

public sealed record CreateBookingBody(
    Guid ScheduleId,
    IReadOnlyList<SeatPassengerRequest> Seats);

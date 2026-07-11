using System.Security.Claims;
using BusBooking.Api.Authorization;
using BusBooking.Application.Booking.Commands.CancelBooking;
using BusBooking.Application.Booking.Commands.CreateBooking;
using BusBooking.Application.Booking.Queries.GetUserBookings;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Buses;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;
using Microsoft.AspNetCore.Authorization;

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
        var oidValue = principal.FindFirst("app:userId")?.Value;

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
        var oidValue = principal.FindFirst("app:userId")?.Value;
        if (!Guid.TryParse(oidValue, out var userId))
            return Results.Unauthorized();

        var handler = new GetUserBookingsHandler(bookingRepo);
        var dtos = await handler.HandleAsync(new GetUserBookingsQuery(userId), ct);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetBookingById(
        Guid bookingId,
        ClaimsPrincipal principal,
        IAuthorizationService authorization,
        IBookingRepository bookingRepo,
        IScheduleRepository scheduleRepo,
        IBusRepository busRepo,
        IRouteRepository routeRepo,
        CancellationToken ct)
    {
        var booking = await bookingRepo.GetByIdReadOnlyAsync(bookingId, ct);
        if (booking is null) return Results.NotFound();

        var authResult = await authorization.AuthorizeAsync(principal, booking, "SameOwner");
        if (!authResult.Succeeded) return Results.Forbid();

        var dto = await BookingDtoFactory.CreateAsync(booking, scheduleRepo, busRepo, routeRepo, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetUserBookings(
        Guid userId,
        ClaimsPrincipal principal,
        IAuthorizationService authorization,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var authResult = await authorization.AuthorizeAsync(principal, new UserIdResource(userId), "SameOwner");
        if (!authResult.Succeeded) return Results.Forbid();

        var handler = new GetUserBookingsHandler(bookingRepo);
        var dtos = await handler.HandleAsync(new GetUserBookingsQuery(userId), ct);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> CancelBooking(
        Guid bookingId,
        HttpContext httpContext,
        IBookingRepository bookingRepo,
        IScheduleRepository scheduleRepo,
        CancellationToken ct)
    {
        var userIdClaim = httpContext.User.FindFirst("app:userId")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Results.Unauthorized();

        var handler = new CancelBookingHandler(bookingRepo, scheduleRepo);
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

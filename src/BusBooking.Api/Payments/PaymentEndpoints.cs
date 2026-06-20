using System.Security.Claims;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Payments;
using BusBooking.Application.Payments.Commands.ProcessPayment;
using BusBooking.Application.Payments.Queries.GetPayment;
using BusBooking.Application.Payments.Queries.GetUserPayments;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Api.Payments;

public static class PaymentEndpoints
{
    public static void MapPaymentEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/payments")
            .WithTags("Payments")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/process", ProcessPayment);
        group.MapGet("/{paymentId:guid}", GetPayment);
        group.MapGet("/user/{userId:guid}", GetUserPayments);
    }

    private static async Task<IResult> ProcessPayment(
        ProcessPaymentCommand command,
        HttpContext httpContext,
        IPaymentRepository paymentRepo,
        IBookingRepository bookingRepo,
        IScheduleRepository scheduleRepo,
        IEventPublisher publisher,
        CancellationToken ct)
    {
        if (!GetUserId(httpContext, out var userId)) return Results.Unauthorized();
        if (command.UserId != userId) return Results.Forbid();

        var handler = new ProcessPaymentHandler(paymentRepo, bookingRepo, scheduleRepo, publisher);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/payments/{id}", new { paymentId = id });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetPayment(
        Guid paymentId, HttpContext httpContext,
        IPaymentRepository paymentRepo, IBookingRepository bookingRepo, CancellationToken ct)
    {
        if (!GetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var handler = new GetPaymentHandler(paymentRepo, bookingRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetPaymentQuery(paymentId, userId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetUserPayments(
        Guid userId, IPaymentRepository paymentRepo, CancellationToken ct)
    {
        var handler = new GetUserPaymentsHandler(paymentRepo);
        var payments = await handler.HandleAsync(new GetUserPaymentsQuery(userId), ct);
        return Results.Ok(payments);
    }

    private static bool GetUserId(HttpContext ctx, out Guid userId) =>
        Guid.TryParse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}

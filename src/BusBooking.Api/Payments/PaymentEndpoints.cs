using System.Security.Claims;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Payments;
using BusBooking.Application.Payments.Commands.ProcessPayment;
using BusBooking.Application.Payments.Queries.GetPayment;
using BusBooking.Application.Payments.Queries.GetUserPayments;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Booking.Enums;

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

        group.MapPost("/create-order", CreateOrder);
        group.MapPost("/", ProcessPayment);
        group.MapGet("/user/{userId:guid}", GetUserPayments);
        group.MapGet("/{paymentId:guid}", GetPayment);
    }

    private static async Task<IResult> CreateOrder(     // Creates a Razorpay order for the given booking's total amount.
        CreateOrderBody body,
        HttpContext httpContext,
        IBookingRepository bookingRepo,
        TenantRazorpayService razorpay,
        CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        var booking = await bookingRepo.GetByIdAsync(body.BookingId, ct);
        if (booking is null) return Results.NotFound("Booking not found.");
        if (booking.UserId != userId) return Results.Forbid();

        try
        {
            var order = await razorpay.CreateOrderAsync(booking.TotalAmount, $"booking-{body.BookingId:N}", ct);
            return Results.Ok(order);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> ProcessPayment(     // Verifies the Razorpay signature and confirms the booking after successful payment.
        RazorpayProcessPaymentBody body,
        HttpContext httpContext,
        IPaymentRepository paymentRepo,
        IBookingRepository bookingRepo,
        IScheduleRepository scheduleRepo,
        IEventPublisher publisher,
        TenantRazorpayService razorpay,
        CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        bool signatureValid;
        try
        {
            signatureValid = await razorpay.VerifySignatureAsync(
                body.RazorpayOrderId, body.RazorpayPaymentId, body.RazorpaySignature, ct);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        if (!signatureValid)
            return Results.BadRequest("Payment signature verification failed.");

        var userName = httpContext.User.FindFirst("name")?.Value
                    ?? httpContext.User.FindFirst(ClaimTypes.Name)?.Value
                    ?? httpContext.User.FindFirst("preferred_username")?.Value
                    ?? "";

        var command = new ProcessPaymentCommand(
            body.BookingId, userId, userName, PaymentMethod.UPI, body.RazorpayPaymentId);

        var handler = new ProcessPaymentHandler(paymentRepo, bookingRepo, scheduleRepo, publisher);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/payments/{id}", new { paymentId = id });
        }
        catch (NotFoundException ex)          { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException)   { return Results.Forbid(); }
        catch (InvalidOperationException ex)  { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetPayment(     // Returns payment details for the specified payment, scoped to the authenticated user.
        Guid paymentId, HttpContext httpContext,
        IPaymentRepository paymentRepo, IBookingRepository bookingRepo, CancellationToken ct)
    {
        if (!GetAppUserId(httpContext, out var userId)) return Results.Unauthorized();

        var handler = new GetPaymentHandler(paymentRepo, bookingRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetPaymentQuery(paymentId, userId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex)        { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetUserPayments(     // Returns the payment history for the specified user.
        Guid userId, IPaymentRepository paymentRepo, CancellationToken ct)
    {
        var handler = new GetUserPaymentsHandler(paymentRepo);
        var payments = await handler.HandleAsync(new GetUserPaymentsQuery(userId), ct);
        return Results.Ok(payments);
    }

    private static bool GetAppUserId(HttpContext ctx, out Guid userId)
    {
        var claim = ctx.User.FindFirst("app:userId")?.Value;
        return Guid.TryParse(claim, out userId);
    }
}

public sealed record CreateOrderBody(Guid BookingId);

public sealed record RazorpayProcessPaymentBody(
    Guid BookingId,
    string RazorpayOrderId,
    string RazorpayPaymentId,
    string RazorpaySignature);

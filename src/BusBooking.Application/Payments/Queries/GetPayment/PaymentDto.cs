using BusBooking.Domain.Booking.Enums;

namespace BusBooking.Application.Payments.Queries.GetPayment;

public sealed record PaymentDto(
    Guid PaymentId,
    Guid BookingId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status,
    string? TransactionReference,
    string? GatewayTransactionId,
    DateTime? PaidAt,
    DateTime CreatedAt);

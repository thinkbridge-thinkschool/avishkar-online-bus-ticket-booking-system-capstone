using BusBooking.Domain.Common;

namespace BusBooking.Domain.Booking.Events;

public sealed record PaymentCompletedEvent(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    decimal Amount,
    string TransactionReference) : IDomainEvent;

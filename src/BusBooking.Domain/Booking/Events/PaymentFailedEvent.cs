using BusBooking.Domain.Common;

namespace BusBooking.Domain.Booking.Events;

public sealed record PaymentFailedEvent(
    Guid PaymentId,
    Guid BookingId,
    Guid UserId,
    string Reason) : IDomainEvent;

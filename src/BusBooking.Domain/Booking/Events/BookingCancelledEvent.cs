using BusBooking.Domain.Common;

namespace BusBooking.Domain.Booking.Events;

public sealed record BookingCancelledEvent(
    Guid BookingId,
    string UserEmail,
    Guid ScheduleId,
    IReadOnlyList<int> ReleasedSeatNumbers) : IDomainEvent;

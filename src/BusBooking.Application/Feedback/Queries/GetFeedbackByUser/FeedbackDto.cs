using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Application.Feedback.Queries.GetFeedbackByUser;

public sealed record FeedbackDto(
    Guid FeedbackId,
    Guid UserId,
    Guid BookingId,
    Guid ScheduleId,
    int Rating,
    string Comment,
    FeedbackCategory Category,
    DateTime CreatedAt);

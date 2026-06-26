using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Application.Feedback.Commands.CreateFeedback;

public sealed record CreateFeedbackCommand(
    Guid UserId,
    Guid BookingId,
    Guid ScheduleId,
    int Rating,
    string Comment,
    FeedbackCategory Category);

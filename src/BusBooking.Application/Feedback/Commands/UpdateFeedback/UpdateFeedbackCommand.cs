using BusBooking.Domain.Feedback.Enums;

namespace BusBooking.Application.Feedback.Commands.UpdateFeedback;

public sealed record UpdateFeedbackCommand(Guid FeedbackId, Guid RequestingUserId, int Rating, string Comment, FeedbackCategory Category);

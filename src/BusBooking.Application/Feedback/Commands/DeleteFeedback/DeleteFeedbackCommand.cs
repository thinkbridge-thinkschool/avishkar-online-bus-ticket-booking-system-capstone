namespace BusBooking.Application.Feedback.Commands.DeleteFeedback;

public sealed record DeleteFeedbackCommand(Guid FeedbackId, Guid RequestingUserId);

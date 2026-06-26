using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Feedback.Commands.UpdateFeedback;

public sealed class UpdateFeedbackHandler(IFeedbackRepository feedbackRepo)
{
    public async Task HandleAsync(UpdateFeedbackCommand command, CancellationToken ct = default)
    {
        var entry = await feedbackRepo.GetByIdAsync(command.FeedbackId, ct)
            ?? throw new NotFoundException("FeedbackEntry", command.FeedbackId);

        if (entry.UserId != command.RequestingUserId)
            throw new UnauthorizedAccessException("You do not own this feedback.");

        entry.Update(command.Rating, command.Comment, command.Category);
        await feedbackRepo.SaveChangesAsync(ct);
    }
}

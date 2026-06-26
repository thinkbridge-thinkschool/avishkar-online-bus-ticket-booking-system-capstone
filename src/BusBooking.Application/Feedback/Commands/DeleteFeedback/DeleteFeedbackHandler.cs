using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Feedback.Commands.DeleteFeedback;

public sealed class DeleteFeedbackHandler(IFeedbackRepository feedbackRepo)
{
    public async Task HandleAsync(DeleteFeedbackCommand command, CancellationToken ct = default)
    {
        var entry = await feedbackRepo.GetByIdAsync(command.FeedbackId, ct)
            ?? throw new NotFoundException("FeedbackEntry", command.FeedbackId);

        if (entry.UserId != command.RequestingUserId)
            throw new UnauthorizedAccessException("You do not own this feedback.");

        await feedbackRepo.DeleteAsync(entry, ct);
        await feedbackRepo.SaveChangesAsync(ct);
    }
}

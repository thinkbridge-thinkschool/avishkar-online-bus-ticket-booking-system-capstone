using BusBooking.Application.Feedback.Queries.GetFeedbackByUser;

namespace BusBooking.Application.Feedback.Queries.GetFeedbackBySchedule;

public sealed class GetFeedbackByScheduleHandler(IFeedbackRepository feedbackRepo)
{
    public async Task<IReadOnlyList<FeedbackDto>> HandleAsync(GetFeedbackByScheduleQuery query, CancellationToken ct = default)
    {
        var entries = await feedbackRepo.GetByScheduleIdAsync(query.ScheduleId, ct);
        return entries.Select(e => new FeedbackDto(e.Id, e.UserId, e.BookingId, e.ScheduleId,
                                                    e.Rating, e.Comment, e.Category, e.CreatedAt))
                      .ToList();
    }
}

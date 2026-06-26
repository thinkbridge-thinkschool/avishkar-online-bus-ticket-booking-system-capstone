namespace BusBooking.Application.Feedback.Queries.GetFeedbackByUser;

public sealed class GetFeedbackByUserHandler(IFeedbackRepository feedbackRepo)
{
    public async Task<IReadOnlyList<FeedbackDto>> HandleAsync(GetFeedbackByUserQuery query, CancellationToken ct = default)
    {
        var entries = await feedbackRepo.GetByUserIdAsync(query.UserId, ct);
        return entries.Select(e => new FeedbackDto(e.Id, e.UserId, e.BookingId, e.ScheduleId,
                                                    e.Rating, e.Comment, e.Category, e.CreatedAt))
                      .ToList();
    }
}

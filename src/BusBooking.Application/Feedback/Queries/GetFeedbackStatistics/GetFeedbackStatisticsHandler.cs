namespace BusBooking.Application.Feedback.Queries.GetFeedbackStatistics;

public sealed class GetFeedbackStatisticsHandler(IFeedbackRepository feedbackRepo)
{
    public async Task<FeedbackStatisticsDto> HandleAsync(GetFeedbackStatisticsQuery query, CancellationToken ct = default)
    {
        var entries = await feedbackRepo.GetByScheduleIdAsync(query.ScheduleId, ct);

        if (entries.Count == 0)
            return new FeedbackStatisticsDto(query.ScheduleId, 0, 0.0, 0, 0, 0, 0, 0);

        return new FeedbackStatisticsDto(
            ScheduleId: query.ScheduleId,
            TotalReviews: entries.Count,
            AverageRating: Math.Round(entries.Average(e => e.Rating), 2),
            FiveStarCount: entries.Count(e => e.Rating == 5),
            FourStarCount: entries.Count(e => e.Rating == 4),
            ThreeStarCount: entries.Count(e => e.Rating == 3),
            TwoStarCount: entries.Count(e => e.Rating == 2),
            OneStarCount: entries.Count(e => e.Rating == 1));
    }
}

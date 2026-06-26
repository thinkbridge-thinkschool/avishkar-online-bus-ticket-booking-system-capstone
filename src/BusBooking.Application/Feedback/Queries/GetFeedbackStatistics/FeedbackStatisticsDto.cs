namespace BusBooking.Application.Feedback.Queries.GetFeedbackStatistics;

public sealed record FeedbackStatisticsDto(
    Guid ScheduleId,
    int TotalReviews,
    double AverageRating,
    int FiveStarCount,
    int FourStarCount,
    int ThreeStarCount,
    int TwoStarCount,
    int OneStarCount);

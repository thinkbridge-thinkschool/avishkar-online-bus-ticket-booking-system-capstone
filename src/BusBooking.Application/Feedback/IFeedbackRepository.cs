namespace BusBooking.Application.Feedback;
using BusBooking.Domain.Feedback.Entities;

public interface IFeedbackRepository
{
    Task<FeedbackEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FeedbackEntry?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<FeedbackEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<FeedbackEntry>> GetByScheduleIdAsync(Guid scheduleId, CancellationToken ct = default);
    Task AddAsync(FeedbackEntry entry, CancellationToken ct = default);
    Task DeleteAsync(FeedbackEntry entry, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

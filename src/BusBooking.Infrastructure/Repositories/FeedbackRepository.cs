using BusBooking.Application.Feedback;
using BusBooking.Domain.Feedback.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class FeedbackRepository(BusBookingDbContext db) : IFeedbackRepository
{
    public Task<FeedbackEntry?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.FeedbackEntries.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<FeedbackEntry?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default) =>
        db.FeedbackEntries.FirstOrDefaultAsync(f => f.BookingId == bookingId, ct);

    public async Task<IReadOnlyList<FeedbackEntry>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.FeedbackEntries.Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public async Task<IReadOnlyList<FeedbackEntry>> GetByScheduleIdAsync(Guid scheduleId, CancellationToken ct = default) =>
        await db.FeedbackEntries.Where(f => f.ScheduleId == scheduleId).OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(FeedbackEntry entry, CancellationToken ct = default) =>
        await db.FeedbackEntries.AddAsync(entry, ct);

    public Task DeleteAsync(FeedbackEntry entry, CancellationToken ct = default)
    {
        db.FeedbackEntries.Remove(entry);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

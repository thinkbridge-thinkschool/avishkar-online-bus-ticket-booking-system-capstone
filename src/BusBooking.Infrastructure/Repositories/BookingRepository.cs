using BusBooking.Application.Booking.Repositories;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class BookingRepository(BusBookingDbContext db) : IBookingRepository
{
    public Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public async Task<IReadOnlyList<BookingAggregate>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.Bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync(ct);

    public Task<int> GetTotalCountAsync(CancellationToken ct = default) =>
        db.Bookings.CountAsync(ct);

    public Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default) =>
        db.Bookings.SumAsync(b => b.TotalAmount, ct);

    public async Task<IReadOnlyList<TenantBookingStats>> GetStatsByTenantAsync(CancellationToken ct = default)
    {
        var raw = await db.Bookings
            .GroupBy(b => b.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count(), Revenue = g.Sum(b => b.TotalAmount) })
            .ToListAsync(ct);
        return raw.Select(r => new TenantBookingStats(r.TenantId, r.Count, r.Revenue)).ToList();
    }

    public async Task AddAsync(BookingAggregate booking, CancellationToken ct = default) =>
        await db.Bookings.AddAsync(booking, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // EF's RowVersion on Seat detected that another request already wrote to one of the
            // seats we reserved between our read and our write. Translate to a domain-meaningful
            // exception so the Application layer stays free of EF references.
            throw new InvalidOperationException(
                "One or more seats were taken by a concurrent booking. Please refresh seat availability and try again.");
        }
    }
}

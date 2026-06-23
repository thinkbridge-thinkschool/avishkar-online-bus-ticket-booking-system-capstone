using BusBooking.Application.Booking.Repositories;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeBookingRepository : IBookingRepository
{
    private readonly List<BookingAggregate> _store = [];

    public Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(b => b.Id == bookingId));

    public Task<IReadOnlyList<BookingAggregate>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BookingAggregate>>(
            _store.Where(b => b.UserId == userId).ToList());

    public Task<int> GetTotalCountAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.Count);

    public Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default) =>
        Task.FromResult(_store.Sum(b => b.TotalAmount));

    public Task<IReadOnlyList<TenantBookingStats>> GetStatsByTenantAsync(CancellationToken ct = default)
    {
        var stats = _store
            .GroupBy(b => b.TenantId)
            .Select(g => new TenantBookingStats(g.Key, g.Count(), g.Sum(b => b.TotalAmount)))
            .ToList();
        return Task.FromResult<IReadOnlyList<TenantBookingStats>>(stats);
    }

    public Task AddAsync(BookingAggregate booking, CancellationToken ct = default)
    {
        _store.Add(booking);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}

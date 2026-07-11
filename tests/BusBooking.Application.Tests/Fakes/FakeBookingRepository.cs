using BusBooking.Application.Booking.Queries.GetUserBookings;
using BusBooking.Application.Booking.Repositories;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeBookingRepository : IBookingRepository
{
    private readonly List<BookingAggregate> _store = [];
    public int SaveChangesCallCount { get; private set; }

    public Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(b => b.Id == bookingId));

    public Task<BookingAggregate?> GetByIdReadOnlyAsync(Guid bookingId, CancellationToken ct = default) =>
        GetByIdAsync(bookingId, ct);

    // No schedule/route/bus store in this fake — trip fields stay null, matching
    // BookingDtoFactory's fallback when a booking's schedule can't be found.
    public Task<IReadOnlyList<BookingDto>> GetByUserIdWithDetailsAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<BookingDto>>(_store
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.BookedAt)
            .Select(b => new BookingDto(
                b.Id, b.ScheduleId, b.Status, b.TotalAmount, b.BookedAt,
                b.Seats.Select(s => new BookedSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.SeatPrice, s.PassengerGender)).ToList()))
            .ToList());

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

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}

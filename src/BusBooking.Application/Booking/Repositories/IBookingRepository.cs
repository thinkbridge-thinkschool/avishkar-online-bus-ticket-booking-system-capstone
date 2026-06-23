using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Booking.Repositories;

public sealed record TenantBookingStats(Guid TenantId, int BookingCount, decimal TotalRevenue);

public interface IBookingRepository
{
    Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<BookingAggregate>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantBookingStats>> GetStatsByTenantAsync(CancellationToken ct = default);
    Task AddAsync(BookingAggregate booking, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

using BusBooking.Application.Booking.Queries.GetUserBookings;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Booking.Repositories;

public sealed record TenantBookingStats(Guid TenantId, int BookingCount, decimal TotalRevenue);

public interface IBookingRepository
{
    // Tracked — callers (ProcessPaymentHandler, CancelBookingHandler) mutate and save this entity.
    Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default);
    // AsNoTracking — for callers that only read (ownership checks, single-booking lookups).
    Task<BookingAggregate?> GetByIdReadOnlyAsync(Guid bookingId, CancellationToken ct = default);
    // Single projected query joining Booking→Schedule→Route→Bus, replacing the old
    // GetByUserIdAsync + per-booking BookingDtoFactory loop (an N+1).
    Task<IReadOnlyList<BookingDto>> GetByUserIdWithDetailsAsync(Guid userId, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
    Task<IReadOnlyList<TenantBookingStats>> GetStatsByTenantAsync(CancellationToken ct = default);
    Task AddAsync(BookingAggregate booking, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

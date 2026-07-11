using BusBooking.Application.Booking.Queries.GetUserBookings;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class BookingRepository(BusBookingDbContext db) : IBookingRepository
{
    public Task<BookingAggregate?> GetByIdAsync(Guid bookingId, CancellationToken ct = default) =>
        db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    public Task<BookingAggregate?> GetByIdReadOnlyAsync(Guid bookingId, CancellationToken ct = default) =>
        db.Bookings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bookingId, ct);

    // Single query joining Booking→Schedule→Route→Bus (left joins, mirroring BookingDtoFactory's
    // null-tolerant behavior when a schedule/route/bus no longer exists) instead of the old
    // load-all-bookings-then-3-queries-per-booking N+1.
    public async Task<IReadOnlyList<BookingDto>> GetByUserIdWithDetailsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var rows = await (
            from b in db.Bookings.AsNoTracking()
            where b.UserId == userId
            orderby b.BookedAt descending
            join s in db.Schedules.AsNoTracking() on b.ScheduleId equals s.Id into schedules
            from schedule in schedules.DefaultIfEmpty()
            join r in db.Routes.AsNoTracking() on schedule.RouteId equals r.Id into routes
            from route in routes.DefaultIfEmpty()
            join bus in db.Buses.AsNoTracking() on schedule.BusId equals bus.Id into buses
            from busEntity in buses.DefaultIfEmpty()
            select new { Booking = b, Schedule = schedule, Route = route, Bus = busEntity }
        ).ToListAsync(ct);

        return rows.Select(x => new BookingDto(
            x.Booking.Id,
            x.Booking.ScheduleId,
            x.Booking.Status,
            x.Booking.TotalAmount,
            x.Booking.BookedAt,
            x.Booking.Seats
                .Select(s => new BookedSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.SeatPrice, s.PassengerGender))
                .ToList(),
            x.Route?.Source,
            x.Route?.Destination,
            x.Schedule?.TravelDate,
            x.Schedule?.DepartureTime,
            x.Schedule?.ArrivalTime,
            x.Bus?.BusName,
            x.Bus?.BusNumber))
            .ToList();
    }

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

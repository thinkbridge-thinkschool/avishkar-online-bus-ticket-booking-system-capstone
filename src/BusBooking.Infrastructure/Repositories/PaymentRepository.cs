using BusBooking.Application.Payments;
using BusBooking.Domain.Booking.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class PaymentRepository(BusBookingDbContext db) : IPaymentRepository
{
    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default) =>
        db.Payments.FirstOrDefaultAsync(p => p.BookingId == bookingId, ct);

    public async Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await db.Payments
                .Where(p => db.Bookings.Any(b => b.Id == p.BookingId && b.UserId == userId))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync(ct);

    public async Task AddAsync(Payment payment, CancellationToken ct = default) =>
        await db.Payments.AddAsync(payment, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

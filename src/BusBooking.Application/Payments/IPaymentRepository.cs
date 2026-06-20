namespace BusBooking.Application.Payments;
using BusBooking.Domain.Booking.Entities;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(Payment payment, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

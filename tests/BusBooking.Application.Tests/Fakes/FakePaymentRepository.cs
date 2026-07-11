using BusBooking.Application.Payments;
using BusBooking.Domain.Booking.Entities;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakePaymentRepository : IPaymentRepository
{
    private readonly List<Payment> _store = [];
    public int SaveChangesCallCount { get; private set; }

    public Task<Payment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(p => p.Id == id));

    public Task<Payment?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(p => p.BookingId == bookingId));

    public Task<IReadOnlyList<Payment>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Payment>>([]);

    public Task AddAsync(Payment payment, CancellationToken ct = default)
    {
        _store.Add(payment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}

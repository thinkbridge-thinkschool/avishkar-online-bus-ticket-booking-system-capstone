namespace BusBooking.Application.Buses;

using BusBooking.Domain.Scheduling.Entities;

public interface IBusRepository
{
    Task<Bus?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Bus>> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default);
    Task<bool> ExistsByBusNumberAsync(string busNumber, CancellationToken ct = default);
    Task AddAsync(Bus bus, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

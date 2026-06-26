using BusBooking.Application.Buses;
using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeBusRepository : IBusRepository
{
    private readonly List<Bus> _store = [];

    public Task<Bus?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(b => b.Id == id));

    public Task<IReadOnlyList<Bus>> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Bus>>(_store.Where(b => b.VendorId == vendorId).ToList());

    public Task<bool> ExistsByBusNumberAsync(string busNumber, CancellationToken ct = default) =>
        Task.FromResult(_store.Any(b => b.BusNumber == busNumber));

    public Task AddAsync(Bus bus, CancellationToken ct = default)
    {
        _store.Add(bus);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public IReadOnlyList<Bus> All => _store.AsReadOnly();
}

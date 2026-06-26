using BusBooking.Application.Vendors;
using BusBooking.Domain.Vendor.Aggregates;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeVendorRepository : IVendorRepository
{
    private readonly List<Vendor> _store = [];

    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(v => v.Id == id));

    public Task<Vendor?> GetByEntraObjectIdAsync(string oid, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(v => v.EntraObjectId == oid));

    public Task<Vendor?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(v => v.Email == email));

    public Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Vendor>>(_store.ToList());

    public Task<IReadOnlyList<Vendor>> GetByStatusAsync(VendorStatus status, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Vendor>>(_store.Where(v => v.Status == status).ToList());

    public Task AddAsync(Vendor vendor, CancellationToken ct = default)
    {
        _store.Add(vendor);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public IReadOnlyList<Vendor> All => _store.AsReadOnly();
}

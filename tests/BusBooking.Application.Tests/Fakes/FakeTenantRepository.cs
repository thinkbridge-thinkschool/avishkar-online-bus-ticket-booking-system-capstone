using BusBooking.Application.Tenants;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeTenantRepository : ITenantRepository
{
    private readonly List<Tenant> _store = [];

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(t => t.Id == id));

    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(t => t.Subdomain == subdomain));

    public Task<Tenant?> GetByAdminEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(t => t.AdminEntraObjectId == entraObjectId));

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Tenant>>(_store.ToList());

    public Task<IReadOnlyList<Tenant>> GetByStatusAsync(TenantStatus status, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Tenant>>(_store.Where(t => t.Status == status).ToList());

    public Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        _store.Add(tenant);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public IReadOnlyList<Tenant> All => _store.AsReadOnly();
}

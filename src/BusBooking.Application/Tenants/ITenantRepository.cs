using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tenants;

public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default);
    Task<Tenant?> GetByAdminEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Tenant>> GetByStatusAsync(TenantStatus status, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

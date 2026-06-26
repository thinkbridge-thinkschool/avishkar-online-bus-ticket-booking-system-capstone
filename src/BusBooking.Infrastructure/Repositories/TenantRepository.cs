using BusBooking.Application.Tenants;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Tenants.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class TenantRepository(BusBookingDbContext db) : ITenantRepository
{
    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Subdomain == subdomain, ct);

    public Task<Tenant?> GetByAdminEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.AdminEntraObjectId == entraObjectId, ct);

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default) =>
        await db.Tenants.OrderBy(t => t.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Tenant>> GetByStatusAsync(TenantStatus status, CancellationToken ct = default) =>
        await db.Tenants.Where(t => t.Status == status).OrderBy(t => t.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await db.Tenants.AddAsync(tenant, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

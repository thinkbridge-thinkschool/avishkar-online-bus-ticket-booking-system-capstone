using BusBooking.Application.Tenants.Queries.GetTenantById;

namespace BusBooking.Application.Tenants.Queries.GetAllTenants;

public sealed class GetAllTenantsHandler(ITenantRepository tenantRepo)
{
    public async Task<IReadOnlyList<TenantDto>> HandleAsync(GetAllTenantsQuery query, CancellationToken ct = default)
    {
        var tenants = await tenantRepo.GetAllAsync(ct);
        return tenants.Select(t => new TenantDto(
            t.Id,
            t.Name,
            t.Subdomain,
            t.AdminEmail,
            t.Status.ToString(),
            t.ApprovedAt,
            t.RazorpayKeyId is not null,
            t.CreatedAt)).ToList();
    }
}

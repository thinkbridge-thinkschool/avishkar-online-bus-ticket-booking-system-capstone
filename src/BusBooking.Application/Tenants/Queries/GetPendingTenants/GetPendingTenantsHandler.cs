using BusBooking.Application.Tenants.Queries.GetTenantById;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tenants.Queries.GetPendingTenants;

public sealed class GetPendingTenantsHandler(ITenantRepository tenantRepo)
{
    public async Task<IReadOnlyList<TenantDto>> HandleAsync(GetPendingTenantsQuery query, CancellationToken ct = default)
    {
        var tenants = await tenantRepo.GetByStatusAsync(TenantStatus.PendingApproval, ct);
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

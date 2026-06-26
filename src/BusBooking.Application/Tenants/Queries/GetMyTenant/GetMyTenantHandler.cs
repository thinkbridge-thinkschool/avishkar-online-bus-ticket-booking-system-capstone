using BusBooking.Application.Tenants.Queries.GetTenantById;

namespace BusBooking.Application.Tenants.Queries.GetMyTenant;

public sealed class GetMyTenantHandler(ITenantRepository tenantRepo)
{
    public async Task<TenantDto?> HandleAsync(GetMyTenantQuery query, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByAdminEntraObjectIdAsync(query.AdminEntraObjectId, ct);
        if (tenant is null)
            return null;

        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Subdomain,
            tenant.AdminEmail,
            tenant.Status.ToString(),
            tenant.ApprovedAt,
            tenant.RazorpayKeyId is not null,
            tenant.CreatedAt);
    }
}

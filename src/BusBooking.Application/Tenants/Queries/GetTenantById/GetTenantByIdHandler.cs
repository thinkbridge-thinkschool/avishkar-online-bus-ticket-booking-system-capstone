using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Queries.GetTenantById;

public sealed class GetTenantByIdHandler(ITenantRepository tenantRepo)
{
    public async Task<TenantDto> HandleAsync(GetTenantByIdQuery query, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(query.TenantId, ct)
            ?? throw new NotFoundException("Tenant", query.TenantId);

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

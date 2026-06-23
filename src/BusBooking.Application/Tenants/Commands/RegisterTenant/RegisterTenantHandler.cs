using BusBooking.Domain.Common;
using BusBooking.Domain.Tenants.Aggregates;

namespace BusBooking.Application.Tenants.Commands.RegisterTenant;

public sealed class RegisterTenantHandler(ITenantRepository tenantRepo)
{
    public async Task<Guid> HandleAsync(RegisterTenantCommand command, CancellationToken ct = default)
    {
        var existingBySubdomain = await tenantRepo.GetBySubdomainAsync(command.Subdomain, ct);
        if (existingBySubdomain is not null)
            throw new InvalidOperationException($"Subdomain '{command.Subdomain}' is already taken.");

        var existingByOid = await tenantRepo.GetByAdminEntraObjectIdAsync(command.AdminEntraObjectId, ct);
        if (existingByOid is not null)
            throw new InvalidOperationException("This account has already registered a tenant.");

        Tenant tenant;
        try
        {
            tenant = Tenant.Register(
                command.Name,
                command.Subdomain,
                command.AdminEmail,
                command.AdminEntraObjectId);
        }
        catch (DomainException ex)
        {
            throw new ArgumentException(ex.Message, ex);
        }

        await tenantRepo.AddAsync(tenant, ct);
        await tenantRepo.SaveChangesAsync(ct);

        return tenant.Id;
    }
}

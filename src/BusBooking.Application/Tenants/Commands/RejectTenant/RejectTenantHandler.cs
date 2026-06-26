using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.RejectTenant;

public sealed class RejectTenantHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(RejectTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Reject();

        await tenantRepo.SaveChangesAsync(ct);
    }
}

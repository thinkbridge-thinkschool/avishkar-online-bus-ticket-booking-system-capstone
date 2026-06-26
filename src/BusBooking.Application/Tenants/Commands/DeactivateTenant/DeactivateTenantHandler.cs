using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.DeactivateTenant;

public sealed class DeactivateTenantHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(DeactivateTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Deactivate();

        await tenantRepo.SaveChangesAsync(ct);
    }
}

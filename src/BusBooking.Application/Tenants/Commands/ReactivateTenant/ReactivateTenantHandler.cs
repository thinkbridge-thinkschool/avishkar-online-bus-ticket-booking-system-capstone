using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.ReactivateTenant;

public sealed class ReactivateTenantHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(ReactivateTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Reactivate();

        await tenantRepo.SaveChangesAsync(ct);
    }
}

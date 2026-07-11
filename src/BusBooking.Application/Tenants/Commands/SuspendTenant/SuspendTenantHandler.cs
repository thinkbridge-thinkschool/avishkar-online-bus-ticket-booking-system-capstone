using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.SuspendTenant;

public sealed class SuspendTenantHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(SuspendTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Suspend();

        // TenantSuspendedEvent is turned into an Outbox row by OutboxSavingChangesInterceptor
        // as part of this save.
        await tenantRepo.SaveChangesAsync(ct);
    }
}

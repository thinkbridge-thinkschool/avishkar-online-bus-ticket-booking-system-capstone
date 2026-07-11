using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.ApproveTenant;

public sealed class ApproveTenantHandler(ITenantRepository tenantRepo)
{
    public async Task HandleAsync(ApproveTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Approve();

        // TenantApprovedEvent is turned into an Outbox row by OutboxSavingChangesInterceptor
        // as part of this save.
        await tenantRepo.SaveChangesAsync(ct);
    }
}

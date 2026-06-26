using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Tenants.Commands.SuspendTenant;

public sealed class SuspendTenantHandler(ITenantRepository tenantRepo, IEventPublisher publisher)
{
    public async Task HandleAsync(SuspendTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await tenantRepo.GetByIdAsync(command.TenantId, ct)
            ?? throw new NotFoundException("Tenant", command.TenantId);

        tenant.Suspend();

        await tenantRepo.SaveChangesAsync(ct);

        foreach (var evt in tenant.DomainEvents)
            await publisher.PublishAsync(evt, ct);
        tenant.ClearDomainEvents();
    }
}

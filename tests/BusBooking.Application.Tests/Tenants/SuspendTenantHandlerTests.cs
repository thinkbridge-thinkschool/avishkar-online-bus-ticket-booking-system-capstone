using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.ApproveTenant;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tenants.Commands.SuspendTenant;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Tenants.Enums;
using BusBooking.Domain.Tenants.Events;

namespace BusBooking.Application.Tests.Tenants;

public sealed class SuspendTenantHandlerTests
{
    private static async Task<(FakeTenantRepository, Guid)> SetupActiveTenantAsync()
    {
        var repo = new FakeTenantRepository();
        var id   = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme Bus", "acme", "a@acme.com", "oid-001"));
        await new ApproveTenantHandler(repo)
            .HandleAsync(new ApproveTenantCommand(id));
        return (repo, id);
    }

    [Fact]
    public async Task HandleAsync_ShouldSuspendActiveTenant_AndRaiseEvent()
    {
        var (repo, tenantId) = await SetupActiveTenantAsync();
        var handler          = new SuspendTenantHandler(repo);

        await handler.HandleAsync(new SuspendTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Suspended, repo.All[0].Status);
        // SetupActiveTenantAsync's own Approve already raised (and, in this fake-repo test with
        // no OutboxSavingChangesInterceptor in play, left uncleared) a TenantApprovedEvent —
        // check for the specific event this action raises rather than an exact count.
        Assert.Contains(repo.All[0].DomainEvents, e => e is TenantSuspendedEvent);
    }

    [Fact]
    public async Task HandleAsync_TenantNotFound_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new SuspendTenantHandler(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new SuspendTenantCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_TenantNotActive_ShouldThrow()
    {
        // Tenant is still PendingApproval — suspend must reject it
        var repo = new FakeTenantRepository();
        var id   = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme Bus", "acme", "a@acme.com", "oid-001"));
        var handler = new SuspendTenantHandler(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new SuspendTenantCommand(id)));
    }
}

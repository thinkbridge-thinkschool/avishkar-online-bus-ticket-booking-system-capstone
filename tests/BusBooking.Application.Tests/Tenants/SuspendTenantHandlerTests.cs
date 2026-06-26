using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.ApproveTenant;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tenants.Commands.SuspendTenant;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tests.Tenants;

public sealed class SuspendTenantHandlerTests
{
    private static async Task<(FakeTenantRepository, Guid)> SetupActiveTenantAsync()
    {
        var repo = new FakeTenantRepository();
        var id   = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme Bus", "acme", "a@acme.com", "oid-001"));
        await new ApproveTenantHandler(repo, new FakeEventPublisher())
            .HandleAsync(new ApproveTenantCommand(id));
        return (repo, id);
    }

    [Fact]
    public async Task HandleAsync_ShouldSuspendActiveTenant_AndPublishEvent()
    {
        var (repo, tenantId) = await SetupActiveTenantAsync();
        var publisher        = new FakeEventPublisher();
        var handler          = new SuspendTenantHandler(repo, publisher);

        await handler.HandleAsync(new SuspendTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Suspended, repo.All[0].Status);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task HandleAsync_TenantNotFound_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new SuspendTenantHandler(repo, new FakeEventPublisher());

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
        var handler = new SuspendTenantHandler(repo, new FakeEventPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new SuspendTenantCommand(id)));
    }
}

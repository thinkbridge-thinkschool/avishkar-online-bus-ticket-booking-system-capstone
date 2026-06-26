using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.ApproveTenant;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tests.Tenants;

public sealed class ApproveTenantHandlerTests
{
    private static async Task<(FakeTenantRepository, Guid)> SetupPendingTenantAsync()
    {
        var repo = new FakeTenantRepository();
        var id   = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme Bus", "acme", "a@acme.com", "oid-001"));
        return (repo, id);
    }

    [Fact]
    public async Task HandleAsync_ShouldApproveTenant_AndPublishEvent()
    {
        var (repo, tenantId) = await SetupPendingTenantAsync();
        var publisher        = new FakeEventPublisher();
        var handler          = new ApproveTenantHandler(repo, publisher);

        await handler.HandleAsync(new ApproveTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Active, repo.All[0].Status);
        Assert.NotNull(repo.All[0].ApprovedAt);
        Assert.Single(publisher.Published);
    }

    [Fact]
    public async Task HandleAsync_TenantNotFound_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new ApproveTenantHandler(repo, new FakeEventPublisher());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new ApproveTenantCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_AlreadyActive_ShouldThrow()
    {
        var (repo, tenantId) = await SetupPendingTenantAsync();
        var publisher        = new FakeEventPublisher();
        var handler          = new ApproveTenantHandler(repo, publisher);

        await handler.HandleAsync(new ApproveTenantCommand(tenantId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ApproveTenantCommand(tenantId)));
    }
}

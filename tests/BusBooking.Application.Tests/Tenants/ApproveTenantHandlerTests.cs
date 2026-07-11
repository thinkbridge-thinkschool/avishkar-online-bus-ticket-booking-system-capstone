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
    public async Task HandleAsync_ShouldApproveTenant_AndRaiseEvent()
    {
        var (repo, tenantId) = await SetupPendingTenantAsync();
        var handler          = new ApproveTenantHandler(repo);

        await handler.HandleAsync(new ApproveTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Active, repo.All[0].Status);
        Assert.NotNull(repo.All[0].ApprovedAt);
        // Not cleared here — OutboxSavingChangesInterceptor (a real EF pipeline, not exercised
        // by this fake-repo unit test) is what turns this into an Outbox row and clears it.
        Assert.Single(repo.All[0].DomainEvents);
    }

    [Fact]
    public async Task HandleAsync_TenantNotFound_ShouldThrow()
    {
        var repo    = new FakeTenantRepository();
        var handler = new ApproveTenantHandler(repo);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new ApproveTenantCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_AlreadyActive_ShouldThrow()
    {
        var (repo, tenantId) = await SetupPendingTenantAsync();
        var handler          = new ApproveTenantHandler(repo);

        await handler.HandleAsync(new ApproveTenantCommand(tenantId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ApproveTenantCommand(tenantId)));
    }
}

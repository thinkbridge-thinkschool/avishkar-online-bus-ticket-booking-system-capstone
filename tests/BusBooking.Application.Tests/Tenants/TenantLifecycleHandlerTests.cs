using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants.Commands.ApproveTenant;
using BusBooking.Application.Tenants.Commands.ReactivateTenant;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tenants.Commands.RejectTenant;
using BusBooking.Application.Tenants.Commands.SuspendTenant;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Tenants.Enums;

namespace BusBooking.Application.Tests.Tenants;

public sealed class TenantLifecycleHandlerTests
{
    [Fact]
    public async Task RejectTenantHandler_ShouldRejectPendingTenant()
    {
        var repo = new FakeTenantRepository();
        var tenantId = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme", "acme", "admin@acme.com", "oid-001"));
        var handler = new RejectTenantHandler(repo);

        await handler.HandleAsync(new RejectTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Rejected, repo.All[0].Status);
    }

    [Fact]
    public async Task RejectTenantHandler_WhenTenantMissing_ShouldThrowNotFound()
    {
        var handler = new RejectTenantHandler(new FakeTenantRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new RejectTenantCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task ReactivateTenantHandler_ShouldReactivateSuspendedTenant()
    {
        var repo = new FakeTenantRepository();
        var tenantId = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme", "acme", "admin@acme.com", "oid-001"));
        await new ApproveTenantHandler(repo)
            .HandleAsync(new ApproveTenantCommand(tenantId));
        await new SuspendTenantHandler(repo)
            .HandleAsync(new SuspendTenantCommand(tenantId));

        var handler = new ReactivateTenantHandler(repo);
        await handler.HandleAsync(new ReactivateTenantCommand(tenantId));

        Assert.Equal(TenantStatus.Active, repo.All[0].Status);
    }

    [Fact]
    public async Task ReactivateTenantHandler_WhenNotSuspended_ShouldThrow()
    {
        var repo = new FakeTenantRepository();
        var tenantId = await new RegisterTenantHandler(repo)
            .HandleAsync(new RegisterTenantCommand("Acme", "acme", "admin@acme.com", "oid-001"));
        var handler = new ReactivateTenantHandler(repo);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ReactivateTenantCommand(tenantId)));
    }
}

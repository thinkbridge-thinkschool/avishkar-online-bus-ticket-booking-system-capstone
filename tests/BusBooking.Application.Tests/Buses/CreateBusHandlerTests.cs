using BusBooking.Application.Buses.Commands.CreateBus;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Tests.Buses;

public sealed class CreateBusHandlerTests
{
    private static readonly Guid VendorId = Guid.NewGuid();

    private static CreateBusCommand MakeCommand(string busNumber = "KA-01-1234") =>
        new(VendorId, busNumber, "Express Deluxe", BusType.Sleeper, 40);

    [Fact]
    public async Task HandleAsync_ShouldCreateBusWithTenantId()
    {
        var tenantId = Guid.NewGuid();
        var ctx      = FakeTenantContext.Resolved(tenantId);
        var repo     = new FakeBusRepository();
        var handler  = new CreateBusHandler(repo, ctx);

        var id = await handler.HandleAsync(MakeCommand());

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(repo.All);
        Assert.Equal(tenantId, repo.All[0].TenantId);
    }

    [Fact]
    public async Task HandleAsync_UnresolvedTenant_ShouldThrow()
    {
        var handler = new CreateBusHandler(new FakeBusRepository(), FakeTenantContext.Unresolved());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(MakeCommand()));
    }

    [Fact]
    public async Task HandleAsync_DuplicateBusNumber_ShouldThrow()
    {
        var ctx     = FakeTenantContext.Resolved();
        var repo    = new FakeBusRepository();
        var handler = new CreateBusHandler(repo, ctx);

        await handler.HandleAsync(MakeCommand("KA-01-1234"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(MakeCommand("KA-01-1234")));
    }
}

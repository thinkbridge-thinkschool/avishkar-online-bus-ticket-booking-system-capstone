using BusBooking.Application.Tests.Fakes;
using BusBooking.Application.Vendors.Commands.RegisterVendor;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Tests.Vendors;

public sealed class RegisterVendorHandlerTests
{
    private static RegisterVendorCommand MakeCommand(string oid = "oid-abc") =>
        new RegisterVendorCommand(oid, "QuickBus", "quick@bus.com", "+911111111111", "1 Road", "LIC-999");

    [Fact]
    public async Task HandleAsync_ShouldRegisterVendorAndReturnId()
    {
        var repo = new FakeVendorRepository();
        var handler = new RegisterVendorHandler(repo);

        var id = await handler.HandleAsync(MakeCommand());

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(repo.All);
        Assert.Equal(VendorStatus.Pending, repo.All[0].Status);
        Assert.Equal("QuickBus", repo.All[0].VendorName);
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ShouldThrow()
    {
        var repo = new FakeVendorRepository();
        var handler = new RegisterVendorHandler(repo);

        await handler.HandleAsync(MakeCommand("oid-1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new RegisterVendorCommand("oid-2", "OtherBus", "quick@bus.com", "+912222222222", "2 Road", "LIC-888")));
    }
}

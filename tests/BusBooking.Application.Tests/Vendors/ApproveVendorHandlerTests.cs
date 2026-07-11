using BusBooking.Application.Tests.Fakes;
using BusBooking.Application.Vendors.Commands.ApproveVendor;
using BusBooking.Application.Vendors.Commands.RegisterVendor;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Tests.Vendors;

public sealed class ApproveVendorHandlerTests
{
    private static async Task<(FakeVendorRepository, Guid)> SetupPendingVendorAsync()
    {
        var repo = new FakeVendorRepository();
        var id = await new RegisterVendorHandler(repo)
            .HandleAsync(new RegisterVendorCommand("oid-1", "Bus Co", "b@b.com", "+911234567890", "Addr", "LIC-1"));
        return (repo, id);
    }

    [Fact]
    public async Task HandleAsync_ShouldApproveVendor_AndRaiseEvent()
    {
        var (repo, vendorId) = await SetupPendingVendorAsync();
        var handler = new ApproveVendorHandler(repo, new FakeAppUserRepository(), new FakeTenantRepository());

        await handler.HandleAsync(new ApproveVendorCommand(vendorId));

        Assert.Equal(VendorStatus.Approved, repo.All[0].Status);
        Assert.Single(repo.All[0].DomainEvents);
    }

    [Fact]
    public async Task HandleAsync_VendorNotFound_ShouldThrow()
    {
        var repo = new FakeVendorRepository();
        var handler = new ApproveVendorHandler(repo, new FakeAppUserRepository(), new FakeTenantRepository());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.HandleAsync(new ApproveVendorCommand(Guid.NewGuid())));
    }

    [Fact]
    public async Task HandleAsync_AlreadyApproved_ShouldThrow()
    {
        var (repo, vendorId) = await SetupPendingVendorAsync();
        var handler = new ApproveVendorHandler(repo, new FakeAppUserRepository(), new FakeTenantRepository());

        await handler.HandleAsync(new ApproveVendorCommand(vendorId));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new ApproveVendorCommand(vendorId)));
    }
}

using BusBooking.Domain.Vendor.Enums;
using BusBooking.Domain.Vendor.Events;
using VendorAggregate = BusBooking.Domain.Vendor.Aggregates.Vendor;

namespace BusBooking.Domain.Tests.Vendor;

public sealed class VendorAggregateTests
{
    private static VendorAggregate MakeVendor() =>
        VendorAggregate.Register("oid-123", "FastBus Co", "fast@bus.com", "+911234567890", "123 Main St", "LIC-001");

    [Fact]
    public void Register_ShouldCreatePendingVendor()
    {
        var vendor = MakeVendor();

        Assert.Equal(VendorStatus.Pending, vendor.Status);
        Assert.True(vendor.IsActive);
        Assert.Equal("FastBus Co", vendor.VendorName);
        Assert.Equal("fast@bus.com", vendor.Email);
    }

    [Fact]
    public void Approve_ShouldTransitionToApproved_AndRaiseEvent()
    {
        var vendor = MakeVendor();
        vendor.Approve();

        Assert.Equal(VendorStatus.Approved, vendor.Status);
        var evt = Assert.Single(vendor.DomainEvents);
        Assert.IsType<VendorApprovedEvent>(evt);

        var approved = (VendorApprovedEvent)evt;
        Assert.Equal(vendor.Id, approved.VendorId);
        Assert.Equal("fast@bus.com", approved.Email);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrow()
    {
        var vendor = MakeVendor();
        vendor.Approve();

        Assert.Throws<InvalidOperationException>(() => vendor.Approve());
    }

    [Fact]
    public void Reject_ShouldTransitionToRejected_AndRaiseEvent()
    {
        var vendor = MakeVendor();
        vendor.Reject("Documentation incomplete.");

        Assert.Equal(VendorStatus.Rejected, vendor.Status);
        var evt = Assert.Single(vendor.DomainEvents);
        Assert.IsType<VendorRejectedEvent>(evt);

        var rejected = (VendorRejectedEvent)evt;
        Assert.Equal("Documentation incomplete.", rejected.Reason);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldThrow()
    {
        var vendor = MakeVendor();
        vendor.Reject("reason");

        Assert.Throws<InvalidOperationException>(() => vendor.Reject("reason again"));
    }

    [Fact]
    public void UpdateProfile_ShouldMutateFields()
    {
        var vendor = MakeVendor();
        vendor.UpdateProfile("SpeedBus", "+910000000000", "456 New St");

        Assert.Equal("SpeedBus", vendor.VendorName);
        Assert.Equal("+910000000000", vendor.PhoneNumber);
        Assert.Equal("456 New St", vendor.Address);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var vendor = MakeVendor();
        vendor.Deactivate();

        Assert.False(vendor.IsActive);
    }

    [Fact]
    public void Register_WithEmptyFields_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            VendorAggregate.Register("", "Name", "e@e.com", "1234", "addr", "LIC"));

        Assert.Throws<ArgumentException>(() =>
            VendorAggregate.Register("oid", "", "e@e.com", "1234", "addr", "LIC"));
    }
}

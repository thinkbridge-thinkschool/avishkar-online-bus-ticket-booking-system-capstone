using BusBooking.Domain.Common;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Tenants.Enums;
using BusBooking.Domain.Tenants.Events;

namespace BusBooking.Domain.Tests.Tenant;

public sealed class TenantAggregateTests
{
    // ── Register ────────────────────────────────────────────────────────────

    [Fact]
    public void Register_WithValidData_ShouldCreatePendingTenant()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme Travels", "acme", "admin@acme.com", "oid-123");

        Assert.Equal("Acme Travels", tenant.Name);
        Assert.Equal("acme", tenant.Subdomain);
        Assert.Equal("admin@acme.com", tenant.AdminEmail);
        Assert.Equal("oid-123", tenant.AdminEntraObjectId);
        Assert.Equal(TenantStatus.PendingApproval, tenant.Status);
        Assert.Null(tenant.ApprovedAt);
        Assert.Empty(tenant.DomainEvents);
        Assert.NotEqual(Guid.Empty, tenant.TenantId);
    }

    [Theory]
    [InlineData("ab")]            // too short (2 chars)
    [InlineData("has spaces")]    // spaces are invalid
    [InlineData("-startswithdash")]
    [InlineData("endswithdash-")]
    [InlineData("this-subdomain-is-way-too-long-to-be-valid-xyz")]
    public void Register_WithInvalidSubdomain_ShouldThrowDomainException(string subdomain)
    {
        Assert.Throws<DomainException>(() =>
            Tenants.Aggregates.Tenant.Register("Test", subdomain, "e@e.com", "oid"));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("my-company")]
    [InlineData("acme123")]
    [InlineData("123abc")]
    public void Register_WithValidSubdomains_ShouldSucceed(string subdomain)
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Test", subdomain, "e@e.com", "oid");
        Assert.Equal(subdomain, tenant.Subdomain);
    }

    [Fact]
    public void Register_SubdomainIsNormalisedToLowercase()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Test", "ACME", "e@e.com", "oid");
        Assert.Equal("acme", tenant.Subdomain);
    }

    // ── Approve ─────────────────────────────────────────────────────────────

    [Fact]
    public void Approve_FromPendingApproval_ShouldTransitionToActive_AndRaiseEvent()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        var before = DateTime.UtcNow;

        tenant.Approve();

        Assert.Equal(TenantStatus.Active, tenant.Status);
        Assert.NotNull(tenant.ApprovedAt);
        Assert.True(tenant.ApprovedAt >= before);

        var evt = Assert.Single(tenant.DomainEvents);
        var approved = Assert.IsType<TenantApprovedEvent>(evt);
        Assert.Equal(tenant.TenantId, approved.TenantId);
        Assert.Equal("Acme", approved.TenantName);
    }

    [Fact]
    public void Approve_WhenAlreadyActive_ShouldThrow()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();

        Assert.Throws<InvalidOperationException>(() => tenant.Approve());
    }

    // ── Reject ──────────────────────────────────────────────────────────────

    [Fact]
    public void Reject_FromPendingApproval_ShouldSetRejectedStatus()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Reject();

        Assert.Equal(TenantStatus.Rejected, tenant.Status);
        Assert.Empty(tenant.DomainEvents);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldThrow()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Reject();

        Assert.Throws<InvalidOperationException>(() => tenant.Reject());
    }

    // ── Suspend / Reactivate ─────────────────────────────────────────────────

    [Fact]
    public void Suspend_FromActive_ShouldTransitionToSuspended_AndRaiseEvent()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();
        tenant.ClearDomainEvents();

        tenant.Suspend();

        Assert.Equal(TenantStatus.Suspended, tenant.Status);
        var evt = Assert.Single(tenant.DomainEvents);
        Assert.IsType<TenantSuspendedEvent>(evt);
    }

    [Fact]
    public void Suspend_WhenNotActive_ShouldThrow()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");

        Assert.Throws<InvalidOperationException>(() => tenant.Suspend());
    }

    [Fact]
    public void Reactivate_FromSuspended_ShouldRestoreActiveStatus()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();
        tenant.Suspend();
        tenant.ClearDomainEvents();

        tenant.Reactivate();

        Assert.Equal(TenantStatus.Active, tenant.Status);
    }

    [Fact]
    public void Reactivate_WhenNotSuspended_ShouldThrow()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();

        Assert.Throws<InvalidOperationException>(() => tenant.Reactivate());
    }

    // ── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ShouldSetDeactivatedStatus()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();

        tenant.Deactivate();

        Assert.Equal(TenantStatus.Deactivated, tenant.Status);
    }

    [Fact]
    public void Approve_WhenDeactivated_ShouldThrow()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.Approve();
        tenant.Deactivate();

        Assert.Throws<InvalidOperationException>(() => tenant.Approve());
    }

    // ── Razorpay credentials ─────────────────────────────────────────────────

    [Fact]
    public void SetRazorpayCredentials_ShouldPersistKeys()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.SetRazorpayCredentials("rzp_test_key", "rzp_secret");

        Assert.Equal("rzp_test_key", tenant.RazorpayKeyId);
        Assert.Equal("rzp_secret", tenant.RazorpayKeySecret);
    }

    [Fact]
    public void ClearRazorpayCredentials_ShouldNullOutKeys()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");
        tenant.SetRazorpayCredentials("key", "secret");

        tenant.ClearRazorpayCredentials();

        Assert.Null(tenant.RazorpayKeyId);
        Assert.Null(tenant.RazorpayKeySecret);
    }

    // ── ITenantEntity is NOT applicable to Tenant itself ─────────────────────
    // Entities that carry TenantId implement ITenantEntity; Tenant is the root.

    [Fact]
    public void TenantId_ShouldEqualId()
    {
        var tenant = Tenants.Aggregates.Tenant.Register("Acme", "acme", "a@a.com", "oid");

        Assert.Equal(tenant.Id, tenant.TenantId);
    }
}

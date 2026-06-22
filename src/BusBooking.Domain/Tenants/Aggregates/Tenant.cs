using System.Text.RegularExpressions;
using BusBooking.Domain.Common;
using BusBooking.Domain.Tenants.Enums;
using BusBooking.Domain.Tenants.Events;

namespace BusBooking.Domain.Tenants.Aggregates;

public sealed class Tenant : BaseEntity
{
    // Convenience alias so callers can use tenant.TenantId instead of tenant.Id
    public Guid TenantId => Id;

    public string Name { get; private set; } = default!;

    // Unique lowercase slug used for subdomain routing: acme.busbooking.com
    public string Subdomain { get; private set; } = default!;

    // Entra Object ID of the vendor admin who owns this tenant
    public string AdminEntraObjectId { get; private set; } = default!;

    public string AdminEmail { get; private set; } = default!;

    public TenantStatus Status { get; private set; }

    public DateTime? ApprovedAt { get; private set; }

    // Per-tenant Razorpay credentials; null means use the platform-level default keys
    public string? RazorpayKeyId { get; private set; }
    public string? RazorpayKeySecret { get; private set; }

    private static readonly Regex SubdomainRegex =
        new(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$", RegexOptions.Compiled);

    private Tenant() { }

    public static Tenant Register(string name, string subdomain, string adminEmail, string adminEntraObjectId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(subdomain);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(adminEntraObjectId);

        var slug = subdomain.ToLowerInvariant().Trim();

        if (slug.Length is < 3 or > 30)
            throw new DomainException("Subdomain must be between 3 and 30 characters.");

        if (!SubdomainRegex.IsMatch(slug))
            throw new DomainException("Subdomain may only contain lowercase letters, digits, and hyphens, and must start and end with a letter or digit.");

        return new Tenant
        {
            Name                = name.Trim(),
            Subdomain           = slug,
            AdminEmail          = adminEmail.Trim().ToLowerInvariant(),
            AdminEntraObjectId  = adminEntraObjectId.Trim(),
            Status              = TenantStatus.PendingApproval,
        };
    }

    public void Approve()
    {
        if (Status == TenantStatus.Active)
            throw new InvalidOperationException("Tenant is already active.");

        if (Status == TenantStatus.Deactivated)
            throw new InvalidOperationException("A deactivated tenant cannot be approved.");

        Status     = TenantStatus.Active;
        ApprovedAt = DateTime.UtcNow;
        UpdatedAt  = DateTime.UtcNow;

        RaiseDomainEvent(new TenantApprovedEvent(TenantId, Name, AdminEmail, ApprovedAt.Value));
    }

    public void Reject()
    {
        if (Status == TenantStatus.Rejected)
            throw new InvalidOperationException("Tenant is already rejected.");

        Status    = TenantStatus.Rejected;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        if (Status != TenantStatus.Active)
            throw new InvalidOperationException("Only an active tenant can be suspended.");

        Status    = TenantStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TenantSuspendedEvent(TenantId, Name, DateTime.UtcNow));
    }

    public void Reactivate()
    {
        if (Status != TenantStatus.Suspended)
            throw new InvalidOperationException("Only a suspended tenant can be reactivated.");

        Status    = TenantStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (Status == TenantStatus.Deactivated)
            throw new InvalidOperationException("Tenant is already deactivated.");

        Status    = TenantStatus.Deactivated;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRazorpayCredentials(string keyId, string keySecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(keySecret);

        RazorpayKeyId     = keyId.Trim();
        RazorpayKeySecret = keySecret.Trim();
        UpdatedAt         = DateTime.UtcNow;
    }

    public void ClearRazorpayCredentials()
    {
        RazorpayKeyId     = null;
        RazorpayKeySecret = null;
        UpdatedAt         = DateTime.UtcNow;
    }
}

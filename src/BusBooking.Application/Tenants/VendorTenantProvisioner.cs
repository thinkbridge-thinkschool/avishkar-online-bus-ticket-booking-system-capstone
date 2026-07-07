using System.Text.RegularExpressions;
using BusBooking.Domain.Tenants.Aggregates;

namespace BusBooking.Application.Tenants;

// Vendors never register a "tenant" themselves — an approved vendor needs exactly one
// active tenant to operate (see TenantResolutionMiddleware), so whichever path approves
// a vendor (self-service approval or admin-direct-create) provisions one automatically,
// same shape as DatabaseSeeder's dev vendor. Self-service tenant registration at
// /my-tenant still works for vendors who want a custom subdomain — this is idempotent
// and skips vendors who already have one.
public static class VendorTenantProvisioner
{
    private static readonly Regex NonSlugChars = new("[^a-z0-9-]", RegexOptions.Compiled);

    public static async Task EnsureTenantForVendorAsync(
        string vendorEntraObjectId,
        string vendorName,
        string vendorEmail,
        ITenantRepository tenantRepo,
        CancellationToken ct = default)
    {
        var existing = await tenantRepo.GetByAdminEntraObjectIdAsync(vendorEntraObjectId, ct);
        if (existing is not null)
            return;

        var subdomain = await GenerateUniqueSubdomainAsync(vendorName, vendorEntraObjectId, tenantRepo, ct);

        var tenant = Tenant.Register(vendorName, subdomain, vendorEmail, vendorEntraObjectId);
        tenant.Approve();
        tenant.ClearDomainEvents(); // internal auto-provisioning, not a real approval workflow — no notification

        await tenantRepo.AddAsync(tenant, ct);
        await tenantRepo.SaveChangesAsync(ct);
    }

    private static async Task<string> GenerateUniqueSubdomainAsync(
        string vendorName, string vendorEntraObjectId, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var raw = NonSlugChars.Replace(vendorName.Trim().ToLowerInvariant().Replace(' ', '-'), "").Trim('-');
        if (raw.Length > 24)
            raw = raw[..24].Trim('-');
        if (raw.Length < 3)
            raw = string.IsNullOrEmpty(raw) ? "vendor" : $"vendor-{raw}";

        if (await tenantRepo.GetBySubdomainAsync(raw, ct) is null)
            return raw;

        var suffix = vendorEntraObjectId.Replace("-", "")[..8];
        return $"{raw[..Math.Min(raw.Length, 21)].Trim('-')}-{suffix}";
    }
}

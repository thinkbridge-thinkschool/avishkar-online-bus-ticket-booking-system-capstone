using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace BusBooking.Infrastructure.Identity;

// Runs automatically after UseAuthentication() validates any JWT.
// Adds the "app:userId" claim so all endpoint handlers read the same claim
// regardless of which auth provider issued the token.
//
// MSAL path : oid claim present → app:userId = oid (same value, just normalised name).
//             AppUser record is auto-provisioned on first login.
// Local path : app:userId already embedded in the locally-issued JWT; no action needed.
//
// Key invariant: for MSAL users, AppUser.Id == Guid.Parse(oid), so all existing
// data keyed on the OID (Bookings.UserId, Vendor.EntraObjectId, etc.) remains valid.
internal sealed class AppClaimsTransformer(
    BusBookingDbContext db,
    IMemoryCache cache) : IClaimsTransformation
{
    private const string AppUserIdClaim = "app:userId";
    private const string OidLongForm    = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Local JWT already carries app:userId — nothing to do
        if (principal.HasClaim(c => c.Type == AppUserIdClaim))
            return principal;

        // Only MSAL tokens carry the oid claim
        var oid = principal.FindFirst(OidLongForm)?.Value
               ?? principal.FindFirst("oid")?.Value;
        if (oid is null) return principal;

        // Auto-provision once per OID per cache window
        var cacheKey = $"appuser_provisioned:{oid}";
        if (!cache.TryGetValue(cacheKey, out _))
        {
            await EnsureUserProvisionedAsync(oid, principal);
            cache.Set(cacheKey, true, TimeSpan.FromMinutes(10));
        }

        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim(AppUserIdClaim, oid));
        principal.AddIdentity(identity);
        return principal;
    }

    private async Task EnsureUserProvisionedAsync(string oid, ClaimsPrincipal principal)
    {
        if (!Guid.TryParse(oid, out var oidGuid)) return;

        // Fast primary-key lookup — no EF tracking needed
        var existing = await db.AppUsers.FindAsync(oidGuid);
        if (existing is not null) return;

        var email = principal.FindFirst("preferred_username")?.Value
                 ?? principal.FindFirst("email")?.Value
                 ?? principal.FindFirst("upn")?.Value
                 ?? string.Empty;
        var displayName = principal.FindFirst("name")?.Value
                       ?? email;

        var user  = AppUser.Create(oidGuid, email, displayName, emailVerified: true);
        var login = ExternalLogin.Create(oidGuid, LoginProvider.Entra, oid);

        await db.AppUsers.AddAsync(user);
        await db.ExternalLogins.AddAsync(login);

        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            // Concurrent request for the same user already inserted the record — safe to swallow
        }
    }
}

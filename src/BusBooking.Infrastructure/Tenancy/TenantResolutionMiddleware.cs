using BusBooking.Domain.Tenants.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Tenancy;

// Resolution priority: subdomain → X-Tenant-Id header → JWT tenant_id claim → DB lookup by Entra oid.
// Runs after UseAuthentication so JWT claims are already populated on HttpContext.User.
// If no signal matches an active tenant, IsResolved stays false — treated as Super Admin / platform request.
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        TenantContext tenantContext,
        BusBookingDbContext db)
    {
        await TryResolveAsync(httpContext, tenantContext, db);
        await next(httpContext);
    }

    private static async Task TryResolveAsync(
        HttpContext httpContext,
        TenantContext tenantContext,
        BusBookingDbContext db)
    {
        // 1. Subdomain — e.g. acme.api.busbooking.com or acme.localhost
        var subdomain = ExtractSubdomain(httpContext.Request);
        if (subdomain != null)
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain && t.Status == TenantStatus.Active);
            if (tenant != null)
            {
                tenantContext.Resolve(tenant.Id, tenant.Subdomain);
                return;
            }
        }

        // 2. X-Tenant-Id header — explicit GUID sent by API clients / dev tooling
        if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var headerValue) &&
            Guid.TryParse(headerValue, out var headerTenantId))
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == headerTenantId && t.Status == TenantStatus.Active);
            if (tenant != null)
            {
                tenantContext.Resolve(tenant.Id, tenant.Subdomain);
                return;
            }
        }

        // 3. JWT tenant_id claim — custom claim added by the Entra app registration
        var tenantIdClaim = httpContext.User.FindFirst("tenant_id")?.Value;
        if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim, out var claimTenantId))
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == claimTenantId && t.Status == TenantStatus.Active);
            if (tenant != null)
            {
                tenantContext.Resolve(tenant.Id, tenant.Subdomain);
                return;
            }
        }

        // 4. DB lookup by app:userId — set by AppClaimsTransformer after UseAuthentication.
        // For MSAL users, app:userId == Entra OID, so AdminEntraObjectId lookup still works.
        // For local-auth users (Phase 3+), app:userId is their internal GUID stored in same column.
        var appUserId = httpContext.User.FindFirst("app:userId")?.Value;
        if (appUserId != null)
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.AdminEntraObjectId == appUserId && t.Status == TenantStatus.Active);
            if (tenant != null)
            {
                tenantContext.Resolve(tenant.Id, tenant.Subdomain);
                return;
            }
        }

        // Not resolved — Super Admin, health-check, or anonymous request; query filters disabled.
    }

    // Extracts the leftmost hostname segment as a potential tenant subdomain.
    // Returns null if the host has no subdomain (e.g. plain "localhost").
    private static string? ExtractSubdomain(HttpRequest request)
    {
        var host = request.Host.Host;   // host only, no port
        var dotIndex = host.IndexOf('.');
        if (dotIndex <= 0)
            return null;

        var candidate = host[..dotIndex];
        return candidate.Length is >= 3 and <= 30 ? candidate : null;
    }
}

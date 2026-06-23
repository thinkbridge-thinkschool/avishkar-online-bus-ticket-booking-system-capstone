using BusBooking.Application.Common;

namespace BusBooking.Infrastructure.Tenancy;

// Scoped per-request; the middleware calls Resolve() and application/EF layers read via ITenantContext.
public sealed class TenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public string Subdomain { get; private set; } = string.Empty;
    public bool IsResolved { get; private set; }

    internal void Resolve(Guid tenantId, string subdomain)
    {
        TenantId   = tenantId;
        Subdomain  = subdomain;
        IsResolved = true;
    }
}

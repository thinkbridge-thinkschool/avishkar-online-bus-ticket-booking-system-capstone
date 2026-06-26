using BusBooking.Application.Common;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public string Subdomain { get; set; } = string.Empty;
    public bool IsResolved { get; set; }

    public static FakeTenantContext Resolved(Guid? tenantId = null) => new()
    {
        TenantId   = tenantId ?? Guid.NewGuid(),
        Subdomain  = "test",
        IsResolved = true,
    };

    public static FakeTenantContext Unresolved() => new() { IsResolved = false };
}

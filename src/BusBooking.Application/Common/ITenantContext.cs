namespace BusBooking.Application.Common;

public interface ITenantContext
{
    Guid TenantId { get; }
    string Subdomain { get; }

    // False when the request has no tenant signal (Super Admin, health checks, anonymous).
    // Query filters are disabled when IsResolved is false so Super Admin sees all rows.
    bool IsResolved { get; }
}

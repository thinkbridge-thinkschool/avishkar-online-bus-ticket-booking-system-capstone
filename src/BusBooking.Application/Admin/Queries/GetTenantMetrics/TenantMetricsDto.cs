namespace BusBooking.Application.Admin.Queries.GetTenantMetrics;

public sealed record TenantMetricsDto(
    Guid TenantId,
    string Name,
    string Subdomain,
    string Status,
    int BookingCount,
    decimal TotalRevenue);

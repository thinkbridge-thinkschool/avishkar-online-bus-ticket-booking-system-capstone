using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Tenants;

namespace BusBooking.Application.Admin.Queries.GetTenantMetrics;

public sealed class GetTenantMetricsHandler(
    ITenantRepository tenantRepo,
    IBookingRepository bookingRepo)
{
    public async Task<IReadOnlyList<TenantMetricsDto>> HandleAsync(
        GetTenantMetricsQuery query, CancellationToken ct = default)
    {
        var tenants = await tenantRepo.GetAllAsync(ct);
        var stats   = await bookingRepo.GetStatsByTenantAsync(ct);

        var statsMap = stats.ToDictionary(s => s.TenantId);

        return tenants.Select(t =>
        {
            statsMap.TryGetValue(t.Id, out var s);
            return new TenantMetricsDto(
                t.Id,
                t.Name,
                t.Subdomain,
                t.Status.ToString(),
                s?.BookingCount ?? 0,
                s?.TotalRevenue ?? 0m);
        }).ToList();
    }
}

using BusBooking.Application.Admin.Queries.GetAdminDashboard;
using BusBooking.Application.Admin.Queries.GetTenantMetrics;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Tenants;
using BusBooking.Application.Users;
using BusBooking.Application.Vendors;

namespace BusBooking.Api.Admin;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("SuperAdminOnly")
            .RequireRateLimiting("api");

        group.MapGet("/dashboard", GetDashboard);
        group.MapGet("/tenants/metrics", GetTenantMetrics);
    }

    private static async Task<IResult> GetDashboard(
        IVendorRepository vendorRepo,
        IUserProfileRepository userRepo,
        IBookingRepository bookingRepo,
        ITenantRepository tenantRepo,
        CancellationToken ct)
    {
        var handler = new GetAdminDashboardHandler(vendorRepo, userRepo, bookingRepo, tenantRepo);
        var dto = await handler.HandleAsync(new GetAdminDashboardQuery(), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetTenantMetrics(
        ITenantRepository tenantRepo,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var handler = new GetTenantMetricsHandler(tenantRepo, bookingRepo);
        var metrics = await handler.HandleAsync(new GetTenantMetricsQuery(), ct);
        return Results.Ok(metrics);
    }
}

using BusBooking.Application.Admin.Queries.GetAdminDashboard;
using BusBooking.Application.Booking.Repositories;
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
            .RequireAuthorization("AdminOnly")
            .RequireRateLimiting("api");

        group.MapGet("/dashboard", GetDashboard);
    }

    private static async Task<IResult> GetDashboard(
        IVendorRepository vendorRepo,
        IUserProfileRepository userRepo,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var handler = new GetAdminDashboardHandler(vendorRepo, userRepo, bookingRepo);
        var dto = await handler.HandleAsync(new GetAdminDashboardQuery(), ct);
        return Results.Ok(dto);
    }
}

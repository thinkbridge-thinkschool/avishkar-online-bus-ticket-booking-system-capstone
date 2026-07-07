using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Tenants;
using BusBooking.Application.Users;
using BusBooking.Application.Vendors;
using BusBooking.Domain.Tenants.Enums;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Admin.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardHandler(
    IVendorRepository vendorRepo,
    IUserProfileRepository userRepo,
    IBookingRepository bookingRepo,
    ITenantRepository tenantRepo)
{
    public async Task<AdminDashboardDto> HandleAsync(GetAdminDashboardQuery query, CancellationToken ct = default)
    {
        var vendors  = await vendorRepo.GetAllAsync(ct);
        var users    = await userRepo.GetAllAsync(ct);
        var count    = await bookingRepo.GetTotalCountAsync(ct);
        var revenue  = await bookingRepo.GetTotalRevenueAsync(ct);
        var tenants  = await tenantRepo.GetAllAsync(ct);

        var recentVendors = vendors
            .OrderByDescending(v => v.CreatedAt)
            .Take(5)
            .Select(v => new RecentVendorDto(v.Id, v.VendorName, v.Email, v.Status, v.CreatedAt))
            .ToList();

        return new AdminDashboardDto(
            TotalVendors:     vendors.Count,
            PendingVendors:   vendors.Count(v => v.Status == VendorStatus.Pending),
            ApprovedVendors:  vendors.Count(v => v.Status == VendorStatus.Approved),
            TotalUsers:       users.Count,
            TotalBookings:    count,
            TotalTenants:     tenants.Count,
            PendingTenants:   tenants.Count(t => t.Status == TenantStatus.PendingApproval),
            ActiveTenants:    tenants.Count(t => t.Status == TenantStatus.Active),
            SuspendedTenants: tenants.Count(t => t.Status == TenantStatus.Suspended),
            TotalRevenue:     revenue,
            RecentVendors:    recentVendors);
    }
}

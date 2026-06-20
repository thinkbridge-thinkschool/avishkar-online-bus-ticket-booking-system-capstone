using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Users;
using BusBooking.Application.Vendors;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Admin.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardHandler(
    IVendorRepository vendorRepo,
    IUserProfileRepository userRepo,
    IBookingRepository bookingRepo)
{
    public async Task<AdminDashboardDto> HandleAsync(GetAdminDashboardQuery query, CancellationToken ct = default)
    {
        var vendors = await vendorRepo.GetAllAsync(ct);
        var users = await userRepo.GetAllAsync(ct);

        var totalBookings = 0;
        foreach (var user in users)
        {
            var bookings = await bookingRepo.GetByUserIdAsync(user.Id, ct);
            totalBookings += bookings.Count;
        }

        return new AdminDashboardDto(
            TotalVendors: vendors.Count,
            PendingVendors: vendors.Count(v => v.Status == VendorStatus.Pending),
            ApprovedVendors: vendors.Count(v => v.Status == VendorStatus.Approved),
            TotalUsers: users.Count,
            TotalBookings: totalBookings);
    }
}

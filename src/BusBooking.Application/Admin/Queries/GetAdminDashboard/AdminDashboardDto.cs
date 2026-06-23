namespace BusBooking.Application.Admin.Queries.GetAdminDashboard;

public sealed record AdminDashboardDto(
    int TotalVendors,
    int PendingVendors,
    int ApprovedVendors,
    int TotalUsers,
    int TotalBookings,
    int TotalTenants,
    int PendingTenants,
    int ActiveTenants,
    int SuspendedTenants,
    decimal TotalRevenue);

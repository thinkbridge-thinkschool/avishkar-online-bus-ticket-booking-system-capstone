using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Vendors.Queries.GetVendorProfile;

public sealed record VendorDto(
    Guid VendorId,
    string VendorName,
    string Email,
    string PhoneNumber,
    string Address,
    string LicenseNumber,
    VendorStatus Status,
    bool IsActive);

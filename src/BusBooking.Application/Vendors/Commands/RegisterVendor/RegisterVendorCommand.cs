namespace BusBooking.Application.Vendors.Commands.RegisterVendor;

public sealed record RegisterVendorCommand(
    string EntraObjectId,
    string VendorName,
    string Email,
    string PhoneNumber,
    string Address,
    string LicenseNumber);

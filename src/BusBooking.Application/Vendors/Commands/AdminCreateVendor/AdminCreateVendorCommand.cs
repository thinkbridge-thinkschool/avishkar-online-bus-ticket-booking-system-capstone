namespace BusBooking.Application.Vendors.Commands.AdminCreateVendor;

public sealed record AdminCreateVendorCommand(
    string UserEmail, string VendorName, string PhoneNumber, string Address, string LicenseNumber);

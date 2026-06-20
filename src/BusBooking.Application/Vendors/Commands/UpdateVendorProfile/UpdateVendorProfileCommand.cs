namespace BusBooking.Application.Vendors.Commands.UpdateVendorProfile;

public sealed record UpdateVendorProfileCommand(
    Guid VendorId,
    string RequestingEntraObjectId,
    string VendorName,
    string PhoneNumber,
    string Address);

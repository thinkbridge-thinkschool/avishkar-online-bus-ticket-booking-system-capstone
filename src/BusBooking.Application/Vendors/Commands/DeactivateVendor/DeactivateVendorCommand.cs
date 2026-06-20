namespace BusBooking.Application.Vendors.Commands.DeactivateVendor;

public sealed record DeactivateVendorCommand(Guid VendorId, string RequestingEntraObjectId);

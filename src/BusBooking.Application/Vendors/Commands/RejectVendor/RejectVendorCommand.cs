namespace BusBooking.Application.Vendors.Commands.RejectVendor;

public sealed record RejectVendorCommand(Guid VendorId, string Reason);

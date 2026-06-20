using BusBooking.Domain.Common;

namespace BusBooking.Domain.Vendor.Events;

public sealed record VendorRejectedEvent(Guid VendorId, string VendorName, string Email, string Reason) : IDomainEvent;

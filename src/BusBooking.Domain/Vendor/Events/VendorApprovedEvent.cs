using BusBooking.Domain.Common;

namespace BusBooking.Domain.Vendor.Events;

public sealed record VendorApprovedEvent(Guid VendorId, string VendorName, string Email) : IDomainEvent;

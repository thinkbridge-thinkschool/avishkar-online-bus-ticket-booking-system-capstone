using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Buses.Queries.GetVendorBuses;

public sealed record BusDto(
    Guid BusId,
    string BusNumber,
    string BusName,
    BusType BusType,
    int TotalSeats,
    Guid VendorId,
    bool IsActive);

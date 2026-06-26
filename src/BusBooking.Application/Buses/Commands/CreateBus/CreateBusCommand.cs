using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed record CreateBusCommand(
    Guid VendorId,
    string BusNumber,
    string BusName,
    BusType BusType,
    int TotalSeats);

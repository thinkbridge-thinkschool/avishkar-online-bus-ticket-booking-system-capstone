namespace BusBooking.Application.Buses.Commands.UpdateBus;

public sealed record UpdateBusCommand(Guid BusId, Guid RequestingVendorId, string BusName, int TotalSeats);

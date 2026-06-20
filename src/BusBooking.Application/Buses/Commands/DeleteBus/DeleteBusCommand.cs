namespace BusBooking.Application.Buses.Commands.DeleteBus;

public sealed record DeleteBusCommand(Guid BusId, Guid RequestingVendorId);

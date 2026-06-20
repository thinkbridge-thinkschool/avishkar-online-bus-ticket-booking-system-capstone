namespace BusBooking.Application.Scheduling.Commands.CreateSchedule;

public sealed record CreateScheduleCommand(
    Guid BusId,
    Guid RouteId,
    DateOnly TravelDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    decimal BasePrice,
    Guid RequestingVendorId);

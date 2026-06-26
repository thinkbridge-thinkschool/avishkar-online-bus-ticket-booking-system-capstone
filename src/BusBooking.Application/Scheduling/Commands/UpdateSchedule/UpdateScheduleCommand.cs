namespace BusBooking.Application.Scheduling.Commands.UpdateSchedule;

public sealed record UpdateScheduleCommand(
    Guid ScheduleId,
    Guid RequestingVendorId,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime);

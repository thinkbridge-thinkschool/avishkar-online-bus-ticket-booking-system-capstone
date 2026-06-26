namespace BusBooking.Application.Scheduling.Commands.DeleteSchedule;

public sealed record DeleteScheduleCommand(Guid ScheduleId, Guid RequestingVendorId);

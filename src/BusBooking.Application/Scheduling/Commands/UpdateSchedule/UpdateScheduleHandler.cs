using BusBooking.Application.Buses;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Scheduling.Commands.UpdateSchedule;

public sealed class UpdateScheduleHandler(IScheduleRepository scheduleRepo, IBusRepository busRepo)
{
    public async Task HandleAsync(UpdateScheduleCommand command, CancellationToken ct = default)
    {
        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(command.ScheduleId, ct)
            ?? throw new NotFoundException("Schedule", command.ScheduleId);

        var bus = await busRepo.GetByIdAsync(schedule.BusId, ct)
            ?? throw new NotFoundException("Bus", schedule.BusId);

        if (bus.VendorId != command.RequestingVendorId)
            throw new UnauthorizedAccessException("You do not own this schedule.");

        schedule.UpdateTimes(command.DepartureTime, command.ArrivalTime);
        await scheduleRepo.SaveChangesAsync(ct);
    }
}

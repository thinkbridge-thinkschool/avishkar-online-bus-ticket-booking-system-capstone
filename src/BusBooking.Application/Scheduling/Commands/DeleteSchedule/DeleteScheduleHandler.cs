using BusBooking.Application.Buses;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Scheduling.Commands.DeleteSchedule;

public sealed class DeleteScheduleHandler(IScheduleRepository scheduleRepo, IBusRepository busRepo)
{
    public async Task HandleAsync(DeleteScheduleCommand command, CancellationToken ct = default)
    {
        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(command.ScheduleId, ct)
            ?? throw new NotFoundException("Schedule", command.ScheduleId);

        var bus = await busRepo.GetByIdAsync(schedule.BusId, ct)
            ?? throw new NotFoundException("Bus", schedule.BusId);

        if (bus.VendorId != command.RequestingVendorId)
            throw new UnauthorizedAccessException("You do not own this schedule.");

        schedule.Deactivate();
        await scheduleRepo.SaveChangesAsync(ct);
    }
}

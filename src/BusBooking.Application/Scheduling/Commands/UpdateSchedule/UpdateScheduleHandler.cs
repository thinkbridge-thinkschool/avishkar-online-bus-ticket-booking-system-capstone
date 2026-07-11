using BusBooking.Application.Buses;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;
using Microsoft.Extensions.Logging;

namespace BusBooking.Application.Scheduling.Commands.UpdateSchedule;

public sealed class UpdateScheduleHandler(
    IScheduleRepository scheduleRepo, IBusRepository busRepo, ICacheService cache, ILogger<UpdateScheduleHandler> logger)
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
        await cache.RemoveByTagAsync("schedules", ct);
        logger.LogInformation("Schedule {ScheduleId} times updated", schedule.Id);
    }
}

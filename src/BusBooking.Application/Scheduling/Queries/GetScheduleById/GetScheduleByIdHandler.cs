using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Scheduling.Queries.GetScheduleById;

public sealed class GetScheduleByIdHandler(IScheduleRepository scheduleRepo)
{
    public async Task<ScheduleDetailDto> HandleAsync(GetScheduleByIdQuery query, CancellationToken ct = default)
    {
        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(query.ScheduleId, ct)
            ?? throw new NotFoundException("Schedule", query.ScheduleId);

        return new ScheduleDetailDto(
            schedule.Id, schedule.BusId, schedule.RouteId,
            schedule.TravelDate, schedule.DepartureTime, schedule.ArrivalTime,
            schedule.IsActive, schedule.Seats.Count, schedule.AvailableSeatsCount);
    }
}

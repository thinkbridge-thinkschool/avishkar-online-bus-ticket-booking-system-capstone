using BusBooking.Application.Buses;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Scheduling.Queries.GetVendorSchedules;

public sealed class GetVendorSchedulesHandler(IScheduleRepository scheduleRepo, IBusRepository busRepo)
{
    public async Task<IReadOnlyList<VendorScheduleDto>> HandleAsync(GetVendorSchedulesQuery query, CancellationToken ct = default)
    {
        var buses = await busRepo.GetByVendorIdAsync(query.VendorId, ct);
        var busMap = buses.ToDictionary(b => b.Id);

        var schedules = await scheduleRepo.GetByVendorIdAsync(query.VendorId, ct);

        return schedules
            .Where(s => busMap.ContainsKey(s.BusId))
            .Select(s =>
            {
                var bus = busMap[s.BusId];
                return new VendorScheduleDto(
                    s.Id, s.BusId, bus.BusName, bus.BusNumber,
                    s.RouteId, s.TravelDate, s.DepartureTime, s.ArrivalTime,
                    s.IsActive, bus.TotalSeats, s.AvailableSeatsCount);
            })
            .ToList();
    }
}

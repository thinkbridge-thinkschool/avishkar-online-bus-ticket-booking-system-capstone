using BusBooking.Application.Buses;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Scheduling.Queries.GetVendorSchedules;

public sealed class GetVendorSchedulesHandler(
    IScheduleRepository scheduleRepo, IBusRepository busRepo, IRouteRepository routeRepo)
{
    public async Task<IReadOnlyList<VendorScheduleDto>> HandleAsync(GetVendorSchedulesQuery query, CancellationToken ct = default)
    {
        var buses = await busRepo.GetByVendorIdAsync(query.VendorId, ct);
        var busMap = buses.ToDictionary(b => b.Id);

        var routes = await routeRepo.GetAllAsync(ct);
        var routeMap = routes.ToDictionary(r => r.Id);

        var schedules = await scheduleRepo.GetByVendorIdAsync(query.VendorId, ct);

        return schedules
            .Where(s => busMap.ContainsKey(s.BusId))
            .Select(s =>
            {
                var bus = busMap[s.BusId];
                var route = routeMap.GetValueOrDefault(s.RouteId);
                var minSeatPrice = s.Seats
                    .Where(seat => seat.Status == SeatStatus.Available)
                    .Select(seat => (decimal?)seat.Price)
                    .DefaultIfEmpty()
                    .Min();
                return new VendorScheduleDto(
                    s.Id, s.BusId, bus.BusName, bus.BusNumber,
                    s.RouteId, route?.Source ?? "Unknown", route?.Destination ?? "Unknown",
                    s.TravelDate, s.DepartureTime, s.ArrivalTime,
                    s.IsActive, bus.TotalSeats, s.AvailableSeatsCount, minSeatPrice);
            })
            .ToList();
    }
}

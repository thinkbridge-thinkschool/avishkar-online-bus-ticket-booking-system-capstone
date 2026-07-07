using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Buses;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;

namespace BusBooking.Application.Booking.Queries.GetUserBookings;

public sealed class GetUserBookingsHandler(
    IBookingRepository bookingRepo, IScheduleRepository scheduleRepo, IBusRepository busRepo, IRouteRepository routeRepo)
{
    public async Task<IReadOnlyList<BookingDto>> HandleAsync(
        GetUserBookingsQuery query, CancellationToken ct = default)
    {
        var bookings = await bookingRepo.GetByUserIdAsync(query.UserId, ct);

        var dtos = new List<BookingDto>(bookings.Count);
        foreach (var b in bookings)
            dtos.Add(await BookingDtoFactory.CreateAsync(b, scheduleRepo, busRepo, routeRepo, ct));
        return dtos;
    }
}

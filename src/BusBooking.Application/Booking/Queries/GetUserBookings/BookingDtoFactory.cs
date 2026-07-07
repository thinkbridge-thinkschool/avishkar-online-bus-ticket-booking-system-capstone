using BusBooking.Application.Buses;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Booking.Queries.GetUserBookings;

// Single place that joins a Booking to its Schedule/Bus/Route so every consumer
// (booking list, single booking lookup) returns the same fully-populated ticket data —
// no page has to fall back to one-time query params passed at redirect time.
public static class BookingDtoFactory
{
    public static async Task<BookingDto> CreateAsync(
        BookingAggregate booking,
        IScheduleRepository scheduleRepo,
        IBusRepository busRepo,
        IRouteRepository routeRepo,
        CancellationToken ct = default)
    {
        var seats = booking.Seats
            .Select(s => new BookedSeatDto(s.SeatNumber, s.PassengerName, s.PassengerAge, s.SeatPrice, s.PassengerGender))
            .ToList();

        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(booking.ScheduleId, ct);
        if (schedule is null)
        {
            return new BookingDto(booking.Id, booking.ScheduleId, booking.Status, booking.TotalAmount, booking.BookedAt, seats);
        }

        var bus = await busRepo.GetByIdAsync(schedule.BusId, ct);
        var route = await routeRepo.GetByIdAsync(schedule.RouteId, ct);

        return new BookingDto(
            booking.Id, booking.ScheduleId, booking.Status, booking.TotalAmount, booking.BookedAt, seats,
            route?.Source, route?.Destination, schedule.TravelDate, schedule.DepartureTime, schedule.ArrivalTime,
            bus?.BusName, bus?.BusNumber);
    }
}

using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Booking.ValueObjects;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Booking.Commands.CreateBooking;

public sealed class CreateBookingHandler(
    IScheduleRepository scheduleRepo,
    IBookingRepository bookingRepo)
{
    public async Task<Guid> HandleAsync(CreateBookingCommand command, CancellationToken ct = default)
    {
        // 1. Load schedule with seat inventory
        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(command.ScheduleId, ct)
            ?? throw new NotFoundException("Schedule", command.ScheduleId);

        // 2. Reserve seats atomically — throws if any seat is unavailable or doesn't exist
        var seatNumbers = command.Seats.Select(s => s.SeatNumber).ToList();
        var priceMap = schedule.ReserveSeats(seatNumbers);

        // 3. Map to BookedSeat value objects capturing the price snapshot
        var bookedSeats = command.Seats.Select(s => new BookedSeat(
            s.SeatNumber,
            s.PassengerName,
            s.PassengerAge,
            s.PassengerGender,
            priceMap[s.SeatNumber],
            s.PassengerPhone,
            s.PassengerEmail));

        // 4. Create booking aggregate and move to PaymentPending (payment processed separately)
        var booking = BookingAggregate.Create(command.UserId, command.UserEmail, command.ScheduleId, bookedSeats);
        booking.AwaitPayment();

        // 5. Persist booking (seats remain Reserved until payment confirms them)
        await bookingRepo.AddAsync(booking, ct);
        await bookingRepo.SaveChangesAsync(ct);

        // No domain events to publish — booking awaits payment
        booking.ClearDomainEvents();

        return booking.Id;
    }
}

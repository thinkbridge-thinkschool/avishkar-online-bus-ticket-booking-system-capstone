using BusBooking.Application.Booking.Commands.CreateBooking;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Booking.Enums;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Tests.Booking;

public sealed class CreateBookingHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithAvailableSeat_ShouldCreateBookingAndReserveSeat()
    {
        var scheduleId = Guid.NewGuid();
        var schedule = Schedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 24),
            new TimeOnly(8, 0),
            new TimeOnly(11, 0),
            Guid.NewGuid());
        var seat = Seat.Create(scheduleId, 1, SeatType.Window, 450m);
        schedule.AddSeats([seat]);

        var scheduleRepo = new FakeScheduleRepository { ScheduleForGetByIdWithSeats = schedule };
        var bookingRepo = new FakeBookingRepository();
        var handler = new CreateBookingHandler(scheduleRepo, bookingRepo);

        var bookingId = await handler.HandleAsync(new CreateBookingCommand(
            Guid.NewGuid(),
            "user@example.com",
            "Test User",
            scheduleId,
            [new SeatPassengerRequest(1, "Asha", 30, "Female", null, null)]));

        var saved = await bookingRepo.GetByIdAsync(bookingId);
        Assert.NotNull(saved);
        Assert.Equal(BookingStatus.PaymentPending, saved!.Status);
        Assert.Equal(SeatStatus.Reserved, seat.Status);
    }

    [Fact]
    public async Task HandleAsync_WhenNoSeatsAreAvailable_ShouldThrowInvalidOperationException()
    {
        var scheduleId = Guid.NewGuid();
        var schedule = Schedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 6, 24),
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            Guid.NewGuid());
        var seat = Seat.Create(scheduleId, 4, SeatType.Aisle, 500m);
        seat.Reserve();
        seat.Book();
        schedule.AddSeats([seat]);

        var scheduleRepo = new FakeScheduleRepository { ScheduleForGetByIdWithSeats = schedule };
        var handler = new CreateBookingHandler(scheduleRepo, new FakeBookingRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(new CreateBookingCommand(
            Guid.NewGuid(),
            "user@example.com",
            "Test User",
            scheduleId,
            [new SeatPassengerRequest(4, "Ravi", 28, "Male", null, null)])));
    }
}

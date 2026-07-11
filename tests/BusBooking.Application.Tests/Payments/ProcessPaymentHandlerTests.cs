using BusBooking.Application.Payments.Commands.ProcessPayment;
using BusBooking.Application.Tests.Fakes;
using BusBooking.Domain.Booking.Enums;
using BusBooking.Domain.Booking.Events;
using BusBooking.Domain.Booking.ValueObjects;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Application.Tests.Payments;

public sealed class ProcessPaymentHandlerTests
{
    // Regression test for the transaction fix: the handler used to call both
    // paymentRepo.SaveChangesAsync() and bookingRepo.SaveChangesAsync() — two separate
    // implicit transactions on what is actually the same shared DbContext in production.
    // Now only paymentRepo.SaveChangesAsync() is called; this proves that single call is
    // still enough to persist the payment, the confirmed booking, and the booked seat.
    [Fact]
    public async Task HandleAsync_OnSuccess_ConfirmsBookingAndBooksSeatViaOneSaveCall()
    {
        var scheduleId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var schedule = Schedule.Create(
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 1), new TimeOnly(8, 0), new TimeOnly(12, 0), tenantId);
        var seat = Seat.Create(scheduleId, 1, SeatType.Window, 500m);
        seat.Reserve();
        schedule.AddSeats([seat]);

        var booking = BookingAggregate.Create(
            Guid.NewGuid(), "user@example.com", scheduleId,
            [new BookedSeat(1, "Passenger", 30, "Female", 500m, null, null)], tenantId);
        booking.AwaitPayment();
        booking.ClearDomainEvents();

        var bookingRepo = new FakeBookingRepository();
        await bookingRepo.AddAsync(booking);
        var scheduleRepo = new FakeScheduleRepository { ScheduleForGetByIdWithSeats = schedule };
        var paymentRepo = new FakePaymentRepository();
        var handler = new ProcessPaymentHandler(paymentRepo, bookingRepo, scheduleRepo);

        var command = new ProcessPaymentCommand(booking.Id, booking.UserId, "Test User", PaymentMethod.UPI);
        await handler.HandleAsync(command);

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal(SeatStatus.Booked, seat.Status);
        Assert.Equal(1, paymentRepo.SaveChangesCallCount);
        Assert.Equal(0, bookingRepo.SaveChangesCallCount);
        // Not cleared here — OutboxSavingChangesInterceptor (a real EF pipeline, not exercised
        // by this fake-repo unit test) is what turns this into an Outbox row and clears it.
        Assert.Contains(booking.DomainEvents, e => e is BookingConfirmedEvent);
    }
}

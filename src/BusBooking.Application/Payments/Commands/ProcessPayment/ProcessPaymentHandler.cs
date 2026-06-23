using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Booking.Entities;
using BusBooking.Domain.Booking.Enums;

namespace BusBooking.Application.Payments.Commands.ProcessPayment;

public sealed class ProcessPaymentHandler(
    IPaymentRepository paymentRepo,
    IBookingRepository bookingRepo,
    IScheduleRepository scheduleRepo,
    IEventPublisher publisher)
{
    public async Task<Guid> HandleAsync(ProcessPaymentCommand command, CancellationToken ct = default)
    {
        var booking = await bookingRepo.GetByIdAsync(command.BookingId, ct)
            ?? throw new NotFoundException("Booking", command.BookingId);

        if (booking.UserId != command.UserId)
            throw new UnauthorizedAccessException("You do not own this booking.");

        if (booking.Status != BookingStatus.PaymentPending)
            throw new InvalidOperationException($"Booking is not in a payment-pending state (current: {booking.Status}).");

        var existingPayment = await paymentRepo.GetByBookingIdAsync(command.BookingId, ct);
        if (existingPayment is not null)
            throw new InvalidOperationException("A payment already exists for this booking.");

        // Payment inherits tenant from the booking it settles.
        var payment = Payment.Create(command.BookingId, booking.TotalAmount, command.PaymentMethod, booking.TenantId);

        var gatewayId = command.GatewayTransactionId
            ?? "GW-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant();
        payment.Complete(gatewayId);

        await paymentRepo.AddAsync(payment, ct);

        var schedule = await scheduleRepo.GetByIdWithSeatsAsync(booking.ScheduleId, ct)
            ?? throw new NotFoundException("Schedule", booking.ScheduleId);

        booking.Confirm(command.UserName);

        var seatNumbers = booking.Seats.Select(s => s.SeatNumber).ToList();
        schedule.BookSeats(seatNumbers);

        await paymentRepo.SaveChangesAsync(ct);
        await bookingRepo.SaveChangesAsync(ct);

        foreach (var evt in booking.DomainEvents)
            await publisher.PublishAsync(evt, ct);
        booking.ClearDomainEvents();

        foreach (var evt in payment.DomainEvents)
            await publisher.PublishAsync(evt, ct);
        payment.ClearDomainEvents();

        return payment.Id;
    }
}

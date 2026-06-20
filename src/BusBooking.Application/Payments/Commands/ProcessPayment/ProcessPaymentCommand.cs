using BusBooking.Domain.Booking.Enums;

namespace BusBooking.Application.Payments.Commands.ProcessPayment;

public sealed record ProcessPaymentCommand(
    Guid BookingId,
    Guid UserId,
    string UserName,
    PaymentMethod PaymentMethod);

using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Payments.Queries.GetPayment;

public sealed class GetPaymentHandler(IPaymentRepository paymentRepo, IBookingRepository bookingRepo)
{
    public async Task<PaymentDto> HandleAsync(GetPaymentQuery query, CancellationToken ct = default)
    {
        var payment = await paymentRepo.GetByIdAsync(query.PaymentId, ct)
            ?? throw new NotFoundException("Payment", query.PaymentId);

        var booking = await bookingRepo.GetByIdReadOnlyAsync(payment.BookingId, ct)
            ?? throw new NotFoundException("Booking", payment.BookingId);

        if (booking.UserId != query.RequestingUserId)
            throw new UnauthorizedAccessException("You do not own this payment.");

        return new PaymentDto(payment.Id, payment.BookingId, payment.Amount, payment.Method,
                              payment.Status, payment.TransactionReference, payment.GatewayTransactionId,
                              payment.PaidAt, payment.CreatedAt);
    }
}

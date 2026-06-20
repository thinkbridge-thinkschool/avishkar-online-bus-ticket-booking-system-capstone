using BusBooking.Application.Payments.Queries.GetPayment;

namespace BusBooking.Application.Payments.Queries.GetUserPayments;

public sealed class GetUserPaymentsHandler(IPaymentRepository paymentRepo)
{
    public async Task<IReadOnlyList<PaymentDto>> HandleAsync(GetUserPaymentsQuery query, CancellationToken ct = default)
    {
        var payments = await paymentRepo.GetByUserIdAsync(query.UserId, ct);
        return payments.Select(p => new PaymentDto(p.Id, p.BookingId, p.Amount, p.Method,
                                                    p.Status, p.TransactionReference, p.GatewayTransactionId,
                                                    p.PaidAt, p.CreatedAt))
                       .ToList();
    }
}

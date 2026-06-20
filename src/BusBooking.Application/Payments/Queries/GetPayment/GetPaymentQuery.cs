namespace BusBooking.Application.Payments.Queries.GetPayment;

public sealed record GetPaymentQuery(Guid PaymentId, Guid RequestingUserId);

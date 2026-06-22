using BusBooking.Domain.Booking.Enums;
using BusBooking.Domain.Booking.Events;
using BusBooking.Domain.Common;

namespace BusBooking.Domain.Booking.Entities;

public sealed class Payment : BaseEntity, ITenantEntity
{
    public Guid BookingId { get; private set; }
    public Guid TenantId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public string? TransactionReference { get; private set; }
    public string? GatewayTransactionId { get; private set; }
    public DateTime? PaidAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid bookingId, decimal amount, PaymentMethod method, Guid tenantId)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));

        return new Payment
        {
            BookingId = bookingId,
            TenantId  = tenantId,
            Amount    = amount,
            Method    = method,
            Status    = PaymentStatus.Pending
        };
    }

    public void Complete(string gatewayTransactionId)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment cannot be completed from status: {Status}");

        Status = PaymentStatus.Completed;
        GatewayTransactionId = gatewayTransactionId;
        TransactionReference = "TXN-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        PaidAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentCompletedEvent(Id, BookingId, Guid.Empty, Amount, TransactionReference!));
    }

    public void Fail(string reason)
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Payment cannot be failed from status: {Status}");

        Status = PaymentStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new PaymentFailedEvent(Id, BookingId, Guid.Empty, reason));
    }
}

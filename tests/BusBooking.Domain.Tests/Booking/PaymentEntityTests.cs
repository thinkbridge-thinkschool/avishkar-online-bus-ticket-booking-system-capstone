using BusBooking.Domain.Booking.Entities;
using BusBooking.Domain.Booking.Enums;
using BusBooking.Domain.Booking.Events;

namespace BusBooking.Domain.Tests.Booking;

public sealed class PaymentEntityTests
{
    [Fact]
    public void Create_ShouldHavePendingStatus()
    {
        var payment = Payment.Create(Guid.NewGuid(), 900m, PaymentMethod.UPI, Guid.NewGuid());

        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Equal(900m, payment.Amount);
        Assert.Equal(PaymentMethod.UPI, payment.Method);
        Assert.Null(payment.PaidAt);
        Assert.Null(payment.GatewayTransactionId);
    }

    [Fact]
    public void Create_WithZeroAmount_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(Guid.NewGuid(), 0m, PaymentMethod.UPI, Guid.NewGuid()));
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() => Payment.Create(Guid.NewGuid(), -1m, PaymentMethod.CreditCard, Guid.NewGuid()));
    }

    [Fact]
    public void Complete_ShouldSetCompletedStatus_AndRaiseEvent()
    {
        var payment = Payment.Create(Guid.NewGuid(), 750m, PaymentMethod.CreditCard, Guid.NewGuid());
        var before = DateTime.UtcNow;

        payment.Complete("GW-TESTID123");

        Assert.Equal(PaymentStatus.Completed, payment.Status);
        Assert.Equal("GW-TESTID123", payment.GatewayTransactionId);
        Assert.NotNull(payment.TransactionReference);
        Assert.StartsWith("TXN-", payment.TransactionReference);
        Assert.NotNull(payment.PaidAt);
        Assert.True(payment.PaidAt >= before);

        var evt = Assert.Single(payment.DomainEvents);
        Assert.IsType<PaymentCompletedEvent>(evt);
    }

    [Fact]
    public void Complete_WhenAlreadyCompleted_ShouldThrow()
    {
        var payment = Payment.Create(Guid.NewGuid(), 500m, PaymentMethod.NetBanking, Guid.NewGuid());
        payment.Complete("GW-1");

        Assert.Throws<InvalidOperationException>(() => payment.Complete("GW-2"));
    }

    [Fact]
    public void Fail_ShouldSetFailedStatus_AndRaiseEvent()
    {
        var payment = Payment.Create(Guid.NewGuid(), 500m, PaymentMethod.UPI, Guid.NewGuid());
        payment.Fail("Insufficient funds.");

        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Null(payment.PaidAt);

        var evt = Assert.Single(payment.DomainEvents);
        Assert.IsType<PaymentFailedEvent>(evt);
    }

    [Fact]
    public void Fail_WhenAlreadyCompleted_ShouldThrow()
    {
        var payment = Payment.Create(Guid.NewGuid(), 500m, PaymentMethod.CreditCard, Guid.NewGuid());
        payment.Complete("GW-OK");

        Assert.Throws<InvalidOperationException>(() => payment.Fail("too late"));
    }
}

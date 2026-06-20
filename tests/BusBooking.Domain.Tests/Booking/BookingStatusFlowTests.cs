using BusBooking.Domain.Booking.Enums;
using BusBooking.Domain.Booking.ValueObjects;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Domain.Tests.Booking;

public sealed class BookingStatusFlowTests
{
    private static BookingAggregate MakeBooking() => BookingAggregate.Create(
        Guid.NewGuid(), "user@example.com", Guid.NewGuid(),
        [new BookedSeat(1, "Test User", 30, "Male", 500m, null, null)]);

    [Fact]
    public void AwaitPayment_FromPending_ShouldTransitionToPaymentPending()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();

        Assert.Equal(BookingStatus.PaymentPending, booking.Status);
        Assert.Empty(booking.DomainEvents);
    }

    [Fact]
    public void AwaitPayment_WhenNotPending_ShouldThrow()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();

        Assert.Throws<InvalidOperationException>(() => booking.AwaitPayment());
    }

    [Fact]
    public void Confirm_FromPaymentPending_ShouldTransitionToConfirmed()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();
        booking.ClearDomainEvents();

        booking.Confirm("Admin");

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Single(booking.DomainEvents);
    }

    [Fact]
    public void Confirm_FromPending_ShouldAlsoWork()
    {
        var booking = MakeBooking();
        booking.Confirm("Admin");

        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Fact]
    public void MarkPaymentFailed_FromPaymentPending_ShouldTransitionToPaymentFailed()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();
        booking.MarkPaymentFailed();

        Assert.Equal(BookingStatus.PaymentFailed, booking.Status);
    }

    [Fact]
    public void MarkPaymentFailed_WhenNotPaymentPending_ShouldThrow()
    {
        var booking = MakeBooking();

        Assert.Throws<InvalidOperationException>(() => booking.MarkPaymentFailed());
    }

    [Fact]
    public void Cancel_FromPaymentPending_ShouldBeCancelled()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();
        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Cancel_FromPaymentFailed_ShouldBeCancelled()
    {
        var booking = MakeBooking();
        booking.AwaitPayment();
        booking.MarkPaymentFailed();
        booking.Cancel();

        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Fact]
    public void Complete_FromConfirmed_ShouldBeCompleted()
    {
        var booking = MakeBooking();
        booking.Confirm("Admin");
        booking.Complete();

        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [Fact]
    public void Complete_WhenNotConfirmed_ShouldThrow()
    {
        var booking = MakeBooking();

        Assert.Throws<InvalidOperationException>(() => booking.Complete());
    }
}

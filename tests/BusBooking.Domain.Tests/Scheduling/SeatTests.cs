using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Domain.Tests.Scheduling;

public sealed class SeatTests
{
    [Fact]
    public void Reserve_WhenSeatIsAvailable_ShouldTransitionToReserved()
    {
        var seat = Seat.Create(Guid.NewGuid(), 7, SeatType.Aisle, 399m);

        seat.Reserve();

        Assert.Equal(SeatStatus.Reserved, seat.Status);
        Assert.NotNull(seat.LockedAt);
    }

    [Fact]
    public void Reserve_WhenSeatIsBooked_ShouldThrow()
    {
        var seat = Seat.Create(Guid.NewGuid(), 11, SeatType.Window, 499m);
        seat.Reserve();
        seat.Book();

        Assert.Throws<InvalidOperationException>(() => seat.Reserve());
    }
}

using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Domain.Tests.Scheduling;

public sealed class ScheduleTests
{
    [Fact]
    public void Create_WhenArrivalIsNotAfterDeparture_ShouldThrow()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            Schedule.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateOnly.FromDateTime(DateTime.UtcNow),
                new TimeOnly(10, 0),
                new TimeOnly(10, 0),
                Guid.NewGuid()));

        Assert.Contains("ArrivalTime", ex.Message);
    }

    [Fact]
    public void ReserveSeats_WhenAllRequestedSeatsAreBooked_ShouldThrow()
    {
        var scheduleId = Guid.NewGuid();
        var seat = Seat.Create(scheduleId, 1, SeatType.Window, 450m);
        seat.Reserve();
        seat.Book();

        var schedule = Schedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            new TimeOnly(8, 0),
            new TimeOnly(11, 0),
            Guid.NewGuid());
        schedule.AddSeats([seat]);

        Assert.Throws<InvalidOperationException>(() => schedule.ReserveSeats([1]));
    }
}

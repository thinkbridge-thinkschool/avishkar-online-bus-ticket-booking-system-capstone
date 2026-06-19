using System.ComponentModel.DataAnnotations;

namespace BusBooking.Application.Booking.Commands.CreateBooking;

public sealed record CreateBookingCommand(
    Guid UserId,
    string UserEmail,
    string UserName,
    Guid ScheduleId,
    IReadOnlyList<SeatPassengerRequest> Seats);

public sealed record SeatPassengerRequest(
    [Range(1, 60)]    int    SeatNumber,
    [MaxLength(100)]  string PassengerName,
    [Range(0, 120)]   int    PassengerAge,
    [MaxLength(10)]   string PassengerGender);

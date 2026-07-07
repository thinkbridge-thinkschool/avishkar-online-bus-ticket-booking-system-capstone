using BusBooking.Domain.Booking.Enums;

namespace BusBooking.Application.Booking.Queries.GetUserBookings;

public sealed record BookingDto(
    Guid BookingId,
    Guid ScheduleId,
    BookingStatus Status,
    decimal TotalAmount,
    DateTime BookedAt,
    IReadOnlyList<BookedSeatDto> Seats,
    string? FromCityName = null,
    string? ToCityName = null,
    DateOnly? TravelDate = null,
    TimeOnly? DepartureTime = null,
    TimeOnly? ArrivalTime = null,
    string? BusName = null,
    string? BusNumber = null);

public sealed record BookedSeatDto(
    int SeatNumber,
    string PassengerName,
    int PassengerAge,
    decimal SeatPrice,
    string? PassengerGender = null);

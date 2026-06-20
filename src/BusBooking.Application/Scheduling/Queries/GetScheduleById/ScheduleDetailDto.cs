namespace BusBooking.Application.Scheduling.Queries.GetScheduleById;

public sealed record ScheduleDetailDto(
    Guid ScheduleId,
    Guid BusId,
    Guid RouteId,
    DateOnly TravelDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    bool IsActive,
    int TotalSeats,
    int AvailableSeats);

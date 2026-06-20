namespace BusBooking.Application.Scheduling.Queries.GetVendorSchedules;

public sealed record VendorScheduleDto(
    Guid ScheduleId,
    Guid BusId,
    string BusName,
    string BusNumber,
    Guid RouteId,
    DateOnly TravelDate,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    bool IsActive,
    int TotalSeats,
    int AvailableSeats);

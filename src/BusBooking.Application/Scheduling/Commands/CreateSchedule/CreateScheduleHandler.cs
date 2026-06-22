using BusBooking.Application.Buses;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;

namespace BusBooking.Application.Scheduling.Commands.CreateSchedule;

public sealed class CreateScheduleHandler(IBusRepository busRepo, IScheduleRepository scheduleRepo)
{
    public async Task<Guid> HandleAsync(CreateScheduleCommand command, CancellationToken ct = default)
    {
        if (command.BasePrice <= 0)
            throw new ArgumentException("BasePrice must be greater than zero.");

        var bus = await busRepo.GetByIdAsync(command.BusId, ct)
            ?? throw new NotFoundException("Bus", command.BusId);

        if (bus.VendorId != command.RequestingVendorId)
            throw new UnauthorizedAccessException("You do not own this bus.");

        if (!bus.IsActive)
            throw new InvalidOperationException("Cannot create a schedule for an inactive bus.");

        if (command.TravelDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidOperationException("Travel date cannot be in the past.");

        // TODO(Phase-4): replace Guid.Empty with tenantId from ITenantContext (must match bus.TenantId)
        var schedule = Schedule.Create(command.BusId, command.RouteId, command.TravelDate, command.DepartureTime, command.ArrivalTime, Guid.Empty);

        var seats = Enumerable.Range(1, bus.TotalSeats).Select(n =>
        {
            var seatType = (n % 3) switch
            {
                1 => SeatType.Window,
                2 => SeatType.Middle,
                _ => SeatType.Aisle
            };
            var price = seatType switch
            {
                SeatType.Window => Math.Round(command.BasePrice * 1.2m, 2),
                SeatType.Middle => Math.Round(command.BasePrice * 1.1m, 2),
                _ => Math.Round(command.BasePrice, 2)
            };
            return Seat.Create(schedule.Id, n, seatType, price);
        });

        schedule.AddSeats(seats);

        await scheduleRepo.AddAsync(schedule, ct);
        await scheduleRepo.SaveChangesAsync(ct);

        return schedule.Id;
    }
}

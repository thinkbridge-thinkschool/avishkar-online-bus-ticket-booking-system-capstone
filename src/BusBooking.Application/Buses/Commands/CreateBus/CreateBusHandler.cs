using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed class CreateBusHandler(IBusRepository busRepo)
{
    public async Task<Guid> HandleAsync(CreateBusCommand command, CancellationToken ct = default)
    {
        var exists = await busRepo.ExistsByBusNumberAsync(command.BusNumber, ct);
        if (exists)
            throw new InvalidOperationException($"A bus with number '{command.BusNumber}' already exists.");

        var bus = Bus.Create(command.BusNumber, command.BusName, command.BusType, command.TotalSeats, command.VendorId);
        await busRepo.AddAsync(bus, ct);
        await busRepo.SaveChangesAsync(ct);
        return bus.Id;
    }
}

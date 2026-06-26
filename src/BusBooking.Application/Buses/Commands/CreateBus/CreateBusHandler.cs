using BusBooking.Application.Common;
using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Buses.Commands.CreateBus;

public sealed class CreateBusHandler(IBusRepository busRepo, ITenantContext tenantContext)
{
    public async Task<Guid> HandleAsync(CreateBusCommand command, CancellationToken ct = default)
    {
        if (!tenantContext.IsResolved)
            throw new InvalidOperationException("A resolved tenant is required to create a bus.");

        var exists = await busRepo.ExistsByBusNumberAsync(command.BusNumber, ct);
        if (exists)
            throw new InvalidOperationException($"A bus with number '{command.BusNumber}' already exists.");

        var bus = Bus.Create(command.BusNumber, command.BusName, command.BusType, command.TotalSeats, command.VendorId, tenantContext.TenantId);
        await busRepo.AddAsync(bus, ct);
        await busRepo.SaveChangesAsync(ct);
        return bus.Id;
    }
}

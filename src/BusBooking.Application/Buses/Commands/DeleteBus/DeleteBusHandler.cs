using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Buses.Commands.DeleteBus;

public sealed class DeleteBusHandler(IBusRepository busRepo)
{
    public async Task HandleAsync(DeleteBusCommand command, CancellationToken ct = default)
    {
        var bus = await busRepo.GetByIdAsync(command.BusId, ct)
            ?? throw new NotFoundException("Bus", command.BusId);

        if (bus.VendorId != command.RequestingVendorId)
            throw new UnauthorizedAccessException("You do not own this bus.");

        bus.Deactivate();
        await busRepo.SaveChangesAsync(ct);
    }
}

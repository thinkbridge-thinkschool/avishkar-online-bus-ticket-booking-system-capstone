using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Buses.Commands.UpdateBus;

public sealed class UpdateBusHandler(IBusRepository busRepo)
{
    public async Task HandleAsync(UpdateBusCommand command, CancellationToken ct = default)
    {
        var bus = await busRepo.GetByIdAsync(command.BusId, ct)
            ?? throw new NotFoundException("Bus", command.BusId);

        if (bus.VendorId != command.RequestingVendorId)
            throw new UnauthorizedAccessException("You do not own this bus.");

        bus.UpdateDetails(command.BusName, command.TotalSeats);
        await busRepo.SaveChangesAsync(ct);
    }
}

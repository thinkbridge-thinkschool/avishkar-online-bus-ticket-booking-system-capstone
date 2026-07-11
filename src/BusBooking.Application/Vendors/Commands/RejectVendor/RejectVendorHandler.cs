using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Commands.RejectVendor;

public sealed class RejectVendorHandler(IVendorRepository vendorRepo)
{
    public async Task HandleAsync(RejectVendorCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        vendor.Reject(command.Reason);

        // VendorRejectedEvent is turned into an Outbox row by OutboxSavingChangesInterceptor
        // as part of this save.
        await vendorRepo.SaveChangesAsync(ct);
    }
}

using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Commands.RejectVendor;

public sealed class RejectVendorHandler(IVendorRepository vendorRepo, IEventPublisher publisher)
{
    public async Task HandleAsync(RejectVendorCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        vendor.Reject(command.Reason);

        await vendorRepo.SaveChangesAsync(ct);

        foreach (var evt in vendor.DomainEvents)
            await publisher.PublishAsync(evt, ct);
        vendor.ClearDomainEvents();
    }
}

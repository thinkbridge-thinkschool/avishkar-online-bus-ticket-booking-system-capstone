using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Commands.ApproveVendor;

public sealed class ApproveVendorHandler(IVendorRepository vendorRepo, IEventPublisher publisher)
{
    public async Task HandleAsync(ApproveVendorCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        vendor.Approve();

        await vendorRepo.SaveChangesAsync(ct);

        foreach (var evt in vendor.DomainEvents)
            await publisher.PublishAsync(evt, ct);
        vendor.ClearDomainEvents();
    }
}

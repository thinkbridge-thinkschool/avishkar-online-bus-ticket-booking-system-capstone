using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Commands.DeactivateVendor;

public sealed class DeactivateVendorHandler(IVendorRepository vendorRepo)
{
    public async Task HandleAsync(DeactivateVendorCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        if (vendor.EntraObjectId != command.RequestingEntraObjectId)
            throw new UnauthorizedAccessException("You are not authorized to deactivate this vendor.");

        vendor.Deactivate();

        await vendorRepo.SaveChangesAsync(ct);
    }
}

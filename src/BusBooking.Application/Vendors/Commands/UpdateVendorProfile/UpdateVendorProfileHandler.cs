using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Commands.UpdateVendorProfile;

public sealed class UpdateVendorProfileHandler(IVendorRepository vendorRepo)
{
    public async Task HandleAsync(UpdateVendorProfileCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        if (vendor.EntraObjectId != command.RequestingEntraObjectId)
            throw new UnauthorizedAccessException("You are not authorized to update this vendor profile.");

        vendor.UpdateProfile(command.VendorName, command.PhoneNumber, command.Address);

        await vendorRepo.SaveChangesAsync(ct);
    }
}

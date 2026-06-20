using BusBooking.Application.Common.Exceptions;
using BusBooking.Domain.Vendor.Aggregates;

namespace BusBooking.Application.Vendors.Commands.RegisterVendor;

public sealed class RegisterVendorHandler(IVendorRepository vendorRepo)
{
    public async Task<Guid> HandleAsync(RegisterVendorCommand command, CancellationToken ct = default)
    {
        var existingByEmail = await vendorRepo.GetByEmailAsync(command.Email, ct);
        if (existingByEmail is not null)
            throw new InvalidOperationException($"A vendor with email '{command.Email}' is already registered.");

        var existingByEntra = await vendorRepo.GetByEntraObjectIdAsync(command.EntraObjectId, ct);
        if (existingByEntra is not null)
            throw new InvalidOperationException("This account is already registered as a vendor.");

        var vendor = Vendor.Register(
            command.EntraObjectId,
            command.VendorName,
            command.Email,
            command.PhoneNumber,
            command.Address,
            command.LicenseNumber);

        await vendorRepo.AddAsync(vendor, ct);
        await vendorRepo.SaveChangesAsync(ct);

        return vendor.Id;
    }
}

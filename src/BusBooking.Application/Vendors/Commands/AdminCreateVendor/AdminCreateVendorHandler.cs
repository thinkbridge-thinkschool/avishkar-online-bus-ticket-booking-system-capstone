using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Identity;
using BusBooking.Application.Tenants;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Vendor.Aggregates;

namespace BusBooking.Application.Vendors.Commands.AdminCreateVendor;

public sealed class AdminCreateVendorHandler(IVendorRepository vendorRepo, IAppUserRepository userRepo, ITenantRepository tenantRepo)
{
    private const string VendorRole = "BusBooking.Vendor";

    public async Task<Guid> HandleAsync(AdminCreateVendorCommand command, CancellationToken ct = default)
    {
        var email = command.UserEmail.Trim().ToLowerInvariant();
        var user = await userRepo.GetByEmailAsync(email, ct)
            ?? throw new NotFoundException("User", email);

        var existingByEntra = await vendorRepo.GetByEntraObjectIdAsync(user.Id.ToString(), ct);
        if (existingByEntra is not null)
            throw new InvalidOperationException("This user is already registered as a vendor.");

        var existingByEmail = await vendorRepo.GetByEmailAsync(user.Email, ct);
        if (existingByEmail is not null)
            throw new InvalidOperationException($"A vendor with email '{user.Email}' is already registered.");

        var vendor = Vendor.Register(
            user.Id.ToString(), command.VendorName, user.Email,
            command.PhoneNumber, command.Address, command.LicenseNumber);
        vendor.Approve();
        vendor.ClearDomainEvents(); // admin-direct creation is not a real approval workflow — no notification should fire

        await vendorRepo.AddAsync(vendor, ct);
        await vendorRepo.SaveChangesAsync(ct);

        if (!user.Roles.Any(r => r.RoleName == VendorRole))
        {
            await userRepo.AddRoleAsync(AppUserRole.Create(user.Id, VendorRole), ct);
            await userRepo.SaveChangesAsync(ct);
        }

        await VendorTenantProvisioner.EnsureTenantForVendorAsync(
            vendor.EntraObjectId, vendor.VendorName, vendor.Email, tenantRepo, ct);

        return vendor.Id;
    }
}

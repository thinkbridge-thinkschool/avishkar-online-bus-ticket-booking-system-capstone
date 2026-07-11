using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Identity;
using BusBooking.Application.Tenants;
using BusBooking.Domain.Identity.Entities;

namespace BusBooking.Application.Vendors.Commands.ApproveVendor;

public sealed class ApproveVendorHandler(
    IVendorRepository vendorRepo, IAppUserRepository userRepo, ITenantRepository tenantRepo)
{
    private const string VendorRole = "BusBooking.Vendor";

    public async Task HandleAsync(ApproveVendorCommand command, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(command.VendorId, ct)
            ?? throw new NotFoundException("Vendor", command.VendorId);

        vendor.Approve();

        await vendorRepo.SaveChangesAsync(ct);

        // Approval alone doesn't grant portal access — the "BusBooking.Vendor" role does.
        // Local-auth vendors store their own AppUser.Id as EntraObjectId; grant the role there.
        // (MSAL-linked vendors with no matching AppUser row are skipped — nothing to grant onto.)
        if (Guid.TryParse(vendor.EntraObjectId, out var appUserId))
        {
            var user = await userRepo.GetByIdAsync(appUserId, ct);
            if (user is not null && !user.Roles.Any(r => r.RoleName == VendorRole))
            {
                await userRepo.AddRoleAsync(AppUserRole.Create(appUserId, VendorRole), ct);
                await userRepo.SaveChangesAsync(ct);
            }
        }

        // An approved vendor needs a resolved tenant to manage buses/schedules
        // (see TenantResolutionMiddleware) — provision one automatically so vendors
        // never have to know tenants exist.
        await VendorTenantProvisioner.EnsureTenantForVendorAsync(
            vendor.EntraObjectId, vendor.VendorName, vendor.Email, tenantRepo, ct);

        // VendorApprovedEvent is turned into an Outbox row by OutboxSavingChangesInterceptor
        // as part of vendorRepo.SaveChangesAsync() above.
    }
}

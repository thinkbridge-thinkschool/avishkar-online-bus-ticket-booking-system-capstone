using BusBooking.Application.Vendors.Queries.GetVendorProfile;
using BusBooking.Domain.Vendor.Enums;

namespace BusBooking.Application.Vendors.Queries.GetPendingVendors;

public sealed class GetPendingVendorsHandler(IVendorRepository vendorRepo)
{
    public async Task<IReadOnlyList<VendorDto>> HandleAsync(GetPendingVendorsQuery query, CancellationToken ct = default)
    {
        var vendors = await vendorRepo.GetByStatusAsync(VendorStatus.Pending, ct);
        return vendors.Select(v => new VendorDto(v.Id, v.VendorName, v.Email, v.PhoneNumber,
                                                  v.Address, v.LicenseNumber, v.Status, v.IsActive))
                      .ToList();
    }
}

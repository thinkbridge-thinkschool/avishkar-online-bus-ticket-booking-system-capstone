using BusBooking.Application.Vendors.Queries.GetVendorProfile;

namespace BusBooking.Application.Vendors.Queries.GetAllVendors;

public sealed class GetAllVendorsHandler(IVendorRepository vendorRepo)
{
    public async Task<IReadOnlyList<VendorDto>> HandleAsync(GetAllVendorsQuery query, CancellationToken ct = default)
    {
        var vendors = await vendorRepo.GetAllAsync(ct);
        return vendors.Select(v => new VendorDto(v.Id, v.VendorName, v.Email, v.PhoneNumber,
                                                  v.Address, v.LicenseNumber, v.Status, v.IsActive))
                      .ToList();
    }
}

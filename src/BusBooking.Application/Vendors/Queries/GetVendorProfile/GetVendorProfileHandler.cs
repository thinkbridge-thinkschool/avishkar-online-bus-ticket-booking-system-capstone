using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Vendors.Queries.GetVendorProfile;

public sealed class GetVendorProfileHandler(IVendorRepository vendorRepo)
{
    public async Task<VendorDto> HandleAsync(GetVendorProfileQuery query, CancellationToken ct = default)
    {
        var vendor = await vendorRepo.GetByIdAsync(query.VendorId, ct)
            ?? throw new NotFoundException("Vendor", query.VendorId);

        return new VendorDto(vendor.Id, vendor.VendorName, vendor.Email, vendor.PhoneNumber,
                             vendor.Address, vendor.LicenseNumber, vendor.Status, vendor.IsActive);
    }
}

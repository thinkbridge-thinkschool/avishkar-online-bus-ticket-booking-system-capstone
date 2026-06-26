namespace BusBooking.Application.Vendors;
using BusBooking.Domain.Vendor.Aggregates;
using BusBooking.Domain.Vendor.Enums;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Vendor?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default);
    Task<Vendor?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Vendor>> GetByStatusAsync(VendorStatus status, CancellationToken ct = default);
    Task AddAsync(Vendor vendor, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

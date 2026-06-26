using BusBooking.Application.Vendors;
using BusBooking.Domain.Vendor.Aggregates;
using BusBooking.Domain.Vendor.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class VendorRepository(BusBookingDbContext db) : IVendorRepository
{
    public Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);

    public Task<Vendor?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken ct = default) =>
        db.Vendors.FirstOrDefaultAsync(v => v.EntraObjectId == entraObjectId, ct);

    public Task<Vendor?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Vendors.FirstOrDefaultAsync(v => v.Email == email, ct);

    public async Task<IReadOnlyList<Vendor>> GetAllAsync(CancellationToken ct = default) =>
        await db.Vendors.OrderBy(v => v.VendorName).ToListAsync(ct);

    public async Task<IReadOnlyList<Vendor>> GetByStatusAsync(VendorStatus status, CancellationToken ct = default) =>
        await db.Vendors.Where(v => v.Status == status).OrderBy(v => v.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(Vendor vendor, CancellationToken ct = default) =>
        await db.Vendors.AddAsync(vendor, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

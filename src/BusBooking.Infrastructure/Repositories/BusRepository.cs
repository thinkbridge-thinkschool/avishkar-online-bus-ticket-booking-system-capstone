using BusBooking.Application.Buses;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class BusRepository(BusBookingDbContext db) : IBusRepository
{
    public Task<Bus?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Buses.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<IReadOnlyList<Bus>> GetByVendorIdAsync(Guid vendorId, CancellationToken ct = default) =>
        await db.Buses.Where(b => b.VendorId == vendorId).ToListAsync(ct);

    public Task<bool> ExistsByBusNumberAsync(string busNumber, CancellationToken ct = default) =>
        db.Buses.AnyAsync(b => b.BusNumber == busNumber, ct);

    public async Task AddAsync(Bus bus, CancellationToken ct = default) =>
        await db.Buses.AddAsync(bus, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

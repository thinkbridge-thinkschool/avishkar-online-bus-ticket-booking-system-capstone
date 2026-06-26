using BusBooking.Application.Cities;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class CityRepository(BusBookingDbContext db) : ICityRepository
{
    public Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Cities.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<City?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Cities.FirstOrDefaultAsync(c => c.CityName.ToLower() == name.ToLower(), ct);

    public async Task<IReadOnlyList<City>> GetAllAsync(CancellationToken ct = default) =>
        await db.Cities.OrderBy(c => c.CityName).ToListAsync(ct);

    public async Task AddAsync(City city, CancellationToken ct = default) =>
        await db.Cities.AddAsync(city, ct);

    public Task DeleteAsync(City city, CancellationToken ct = default)
    {
        db.Cities.Remove(city);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

using BusBooking.Application.Cities;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class CityRepository(BusBookingDbContext db) : ICityRepository // CityRepository is a concrete class that implements the ICityRepository interface.
{ // Its job is to talk to the database.Think of it as the bridge between your application and SQL Server.
    // AsNoTracking on all three: GetByIdAsync/GetByNameAsync callers only check for existence
    // (create/delete handlers) — Remove() re-attaches an untracked entity by key automatically,
    // so no tracked read is needed even for the delete path.
    public Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<City?> GetByNameAsync(string name, CancellationToken ct = default) =>
        db.Cities.AsNoTracking().FirstOrDefaultAsync(c => c.CityName.ToLower() == name.ToLower(), ct);

    public async Task<IReadOnlyList<City>> GetAllAsync(CancellationToken ct = default) => // gets all cities from the database and orders them by name. The result is a list of City objects.
        await db.Cities.AsNoTracking().OrderBy(c => c.CityName).ToListAsync(ct);          // This is LINQ converted to SQL query by EF Core. The result is a list of City objects.

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

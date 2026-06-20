using BusBooking.Application.Routes;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Repositories;

internal sealed class RouteRepository(BusBookingDbContext db) : IRouteRepository
{
    public Task<Domain.Scheduling.Entities.Route?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Routes.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Domain.Scheduling.Entities.Route>> GetAllAsync(CancellationToken ct = default) =>
        await db.Routes.OrderBy(r => r.Source).ToListAsync(ct);

    public async Task AddAsync(Domain.Scheduling.Entities.Route route, CancellationToken ct = default) =>
        await db.Routes.AddAsync(route, ct);

    public Task DeleteAsync(Domain.Scheduling.Entities.Route route, CancellationToken ct = default)
    {
        db.Routes.Remove(route);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

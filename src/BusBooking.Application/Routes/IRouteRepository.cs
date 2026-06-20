namespace BusBooking.Application.Routes;

using BusBooking.Domain.Scheduling.Entities;

public interface IRouteRepository
{
    Task<Route?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Route>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Route route, CancellationToken ct = default);
    Task DeleteAsync(Route route, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

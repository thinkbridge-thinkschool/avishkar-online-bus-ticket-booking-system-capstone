namespace BusBooking.Application.Cities;

using BusBooking.Domain.Scheduling.Entities;

public interface ICityRepository
{
    Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<City?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<City>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(City city, CancellationToken ct = default);
    Task DeleteAsync(City city, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

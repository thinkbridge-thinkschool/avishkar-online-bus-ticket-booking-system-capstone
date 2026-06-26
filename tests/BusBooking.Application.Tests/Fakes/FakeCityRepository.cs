using BusBooking.Application.Cities;
using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeCityRepository : ICityRepository
{
    private readonly List<City> _store = [];

    public Task<City?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(c => c.Id == id));

    public Task<City?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(_store.FirstOrDefault(c =>
            c.CityName.Equals(name, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<City>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<City>>(_store.ToList());

    public Task AddAsync(City city, CancellationToken ct = default)
    {
        _store.Add(city);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(City city, CancellationToken ct = default)
    {
        _store.Remove(city);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public IReadOnlyList<City> All => _store.AsReadOnly();
}

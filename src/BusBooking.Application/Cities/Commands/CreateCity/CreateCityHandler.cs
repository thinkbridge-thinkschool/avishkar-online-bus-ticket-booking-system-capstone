using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Cities.Commands.CreateCity;

public sealed class CreateCityHandler(ICityRepository repo)
{
    public async Task<Guid> HandleAsync(CreateCityCommand command, CancellationToken ct = default)
    {
        var existing = await repo.GetByNameAsync(command.CityName, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A city named '{command.CityName}' already exists.");

        var city = City.Create(command.CityName);
        await repo.AddAsync(city, ct);
        await repo.SaveChangesAsync(ct);
        return city.Id;
    }
}

using BusBooking.Application.Common;
using BusBooking.Domain.Scheduling.Entities;
using Microsoft.Extensions.Logging;

namespace BusBooking.Application.Cities.Commands.CreateCity;

public sealed class CreateCityHandler(ICityRepository repo, ICacheService cache, ILogger<CreateCityHandler> logger)
{
    public async Task<Guid> HandleAsync(CreateCityCommand command, CancellationToken ct = default)
    {
        var existing = await repo.GetByNameAsync(command.CityName, ct);
        if (existing is not null)
            throw new InvalidOperationException($"A city named '{command.CityName}' already exists.");

        var city = City.Create(command.CityName);
        await repo.AddAsync(city, ct);
        await repo.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("cities", ct);
        logger.LogInformation("City {CityId} ({CityName}) created", city.Id, city.CityName);
        return city.Id;
    }
}

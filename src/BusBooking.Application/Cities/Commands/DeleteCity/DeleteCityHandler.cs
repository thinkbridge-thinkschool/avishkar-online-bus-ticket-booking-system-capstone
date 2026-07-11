using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace BusBooking.Application.Cities.Commands.DeleteCity;

public sealed class DeleteCityHandler(ICityRepository repo, ICacheService cache, ILogger<DeleteCityHandler> logger)
{
    public async Task HandleAsync(DeleteCityCommand command, CancellationToken ct = default)
    {
        var city = await repo.GetByIdAsync(command.CityId, ct)
            ?? throw new NotFoundException("City", command.CityId);

        await repo.DeleteAsync(city, ct);
        await repo.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("cities", ct);
        logger.LogInformation("City {CityId} ({CityName}) deleted", city.Id, city.CityName);
    }
}

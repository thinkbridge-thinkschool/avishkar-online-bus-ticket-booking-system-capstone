using BusBooking.Application.Common;

namespace BusBooking.Application.Cities.Queries.GetAllCities;

public sealed class GetAllCitiesHandler(ICityRepository repo, ICacheService cache) // contains the business logic to process that request send by query
{
    public async Task<IReadOnlyList<CityDto>> HandleAsync(GetAllCitiesQuery query, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            "cities:all",
            async token =>
            {
                var cities = await repo.GetAllAsync(token);
                return (IReadOnlyList<CityDto>)cities.Select(c => new CityDto(c.Id, c.CityName)).ToList();
            },
            TimeSpan.FromHours(1),
            ["cities"],
            ct);
}

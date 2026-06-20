namespace BusBooking.Application.Cities.Queries.GetAllCities;

public sealed class GetAllCitiesHandler(ICityRepository repo)
{
    public async Task<IReadOnlyList<CityDto>> HandleAsync(GetAllCitiesQuery query, CancellationToken ct = default)
    {
        var cities = await repo.GetAllAsync(ct);
        return cities.Select(c => new CityDto(c.Id, c.CityName)).ToList();
    }
}

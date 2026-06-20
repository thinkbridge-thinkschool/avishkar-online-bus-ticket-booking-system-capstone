using BusBooking.Domain.Common;

namespace BusBooking.Domain.Scheduling.Entities;

public sealed class City : BaseEntity
{
    public string CityName { get; private set; } = default!;

    private City() { }

    public static City Create(string cityName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cityName);
        return new City { CityName = cityName.Trim() };
    }
}

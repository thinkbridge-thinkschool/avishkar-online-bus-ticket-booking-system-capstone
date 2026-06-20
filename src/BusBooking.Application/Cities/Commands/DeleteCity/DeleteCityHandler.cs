using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Cities.Commands.DeleteCity;

public sealed class DeleteCityHandler(ICityRepository repo)
{
    public async Task HandleAsync(DeleteCityCommand command, CancellationToken ct = default)
    {
        var city = await repo.GetByIdAsync(command.CityId, ct)
            ?? throw new NotFoundException("City", command.CityId);

        await repo.DeleteAsync(city, ct);
        await repo.SaveChangesAsync(ct);
    }
}

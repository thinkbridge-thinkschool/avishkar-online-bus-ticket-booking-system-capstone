using BusBooking.Domain.Scheduling.Entities;

namespace BusBooking.Application.Routes.Commands.CreateRoute;

public sealed class CreateRouteHandler(IRouteRepository repo)
{
    public async Task<Guid> HandleAsync(CreateRouteCommand command, CancellationToken ct = default)
    {
        var route = Route.Create(command.Source, command.Destination);
        await repo.AddAsync(route, ct);
        await repo.SaveChangesAsync(ct);
        return route.Id;
    }
}

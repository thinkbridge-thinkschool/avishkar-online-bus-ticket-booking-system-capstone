using BusBooking.Application.Common;
using BusBooking.Domain.Scheduling.Entities;
using Microsoft.Extensions.Logging;

namespace BusBooking.Application.Routes.Commands.CreateRoute;

public sealed class CreateRouteHandler(IRouteRepository repo, ICacheService cache, ILogger<CreateRouteHandler> logger)
{
    public async Task<Guid> HandleAsync(CreateRouteCommand command, CancellationToken ct = default)
    {
        var route = Route.Create(command.Source, command.Destination);
        await repo.AddAsync(route, ct);
        await repo.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("routes", ct);
        logger.LogInformation("Route {RouteId} ({Source} -> {Destination}) created", route.Id, route.Source, route.Destination);
        return route.Id;
    }
}

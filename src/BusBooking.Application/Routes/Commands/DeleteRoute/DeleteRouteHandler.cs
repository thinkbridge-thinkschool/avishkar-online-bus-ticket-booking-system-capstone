using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace BusBooking.Application.Routes.Commands.DeleteRoute;

public sealed class DeleteRouteHandler(IRouteRepository repo, ICacheService cache, ILogger<DeleteRouteHandler> logger)
{
    public async Task HandleAsync(DeleteRouteCommand command, CancellationToken ct = default)
    {
        var route = await repo.GetByIdAsync(command.RouteId, ct)
            ?? throw new NotFoundException("Route", command.RouteId);

        await repo.DeleteAsync(route, ct);
        await repo.SaveChangesAsync(ct);
        await cache.RemoveByTagAsync("routes", ct);
        logger.LogInformation("Route {RouteId} deleted", route.Id);
    }
}

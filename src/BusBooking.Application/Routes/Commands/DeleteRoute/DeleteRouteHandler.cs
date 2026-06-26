using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Application.Routes.Commands.DeleteRoute;

public sealed class DeleteRouteHandler(IRouteRepository repo)
{
    public async Task HandleAsync(DeleteRouteCommand command, CancellationToken ct = default)
    {
        var route = await repo.GetByIdAsync(command.RouteId, ct)
            ?? throw new NotFoundException("Route", command.RouteId);

        await repo.DeleteAsync(route, ct);
        await repo.SaveChangesAsync(ct);
    }
}

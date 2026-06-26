namespace BusBooking.Application.Routes.Queries.GetAllRoutes;

public sealed class GetAllRoutesHandler(IRouteRepository repo)
{
    public async Task<IReadOnlyList<RouteDto>> HandleAsync(GetAllRoutesQuery query, CancellationToken ct = default)
    {
        var routes = await repo.GetAllAsync(ct);
        return routes.Select(r => new RouteDto(r.Id, r.Source, r.Destination)).ToList();
    }
}

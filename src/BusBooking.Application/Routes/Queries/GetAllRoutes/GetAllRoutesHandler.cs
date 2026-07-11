using BusBooking.Application.Common;

namespace BusBooking.Application.Routes.Queries.GetAllRoutes;

public sealed class GetAllRoutesHandler(IRouteRepository repo, ICacheService cache)
{
    public async Task<IReadOnlyList<RouteDto>> HandleAsync(GetAllRoutesQuery query, CancellationToken ct = default) =>
        await cache.GetOrCreateAsync(
            "routes:all",
            async token =>
            {
                var routes = await repo.GetAllAsync(token);
                return (IReadOnlyList<RouteDto>)routes.Select(r => new RouteDto(r.Id, r.Source, r.Destination)).ToList();
            },
            TimeSpan.FromMinutes(30),
            ["routes"],
            ct);
}

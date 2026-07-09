using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Routes;
using BusBooking.Application.Routes.Commands.CreateRoute;
using BusBooking.Application.Routes.Commands.DeleteRoute;
using BusBooking.Application.Routes.Queries.GetAllRoutes;

namespace BusBooking.Api.Routes;

public static class RouteEndpoints
{
    public static void MapRouteEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/routes")
            .WithTags("Routes")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapGet("/", GetAllRoutes);
        group.MapPost("/", CreateRoute).RequireAuthorization("AdminOnly");
        group.MapDelete("/{routeId:guid}", DeleteRoute).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetAllRoutes(IRouteRepository routeRepo, CancellationToken ct)     // Returns all bus routes configured in the system.
    {
        var handler = new GetAllRoutesHandler(routeRepo);
        var routes = await handler.HandleAsync(new GetAllRoutesQuery(), ct);
        return Results.Ok(routes);
    }

    private static async Task<IResult> CreateRoute(     // Creates a new route between two cities (admin only).
        CreateRouteCommand command, IRouteRepository routeRepo, CancellationToken ct)
    {
        var handler = new CreateRouteHandler(routeRepo);
        var id = await handler.HandleAsync(command, ct);
        return Results.Created($"/api/v1/routes/{id}", new { routeId = id });
    }

    private static async Task<IResult> DeleteRoute(     // Removes a route from the system (admin only).
        Guid routeId, IRouteRepository routeRepo, CancellationToken ct)
    {
        var handler = new DeleteRouteHandler(routeRepo);
        try
        {
            await handler.HandleAsync(new DeleteRouteCommand(routeId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }
}

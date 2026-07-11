using BusBooking.Application.Cities;
using BusBooking.Application.Cities.Commands.CreateCity;
using BusBooking.Application.Cities.Commands.DeleteCity;
using BusBooking.Application.Cities.Queries.GetAllCities;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api.Cities;

public static class CityEndpoints
{
    public static void MapCityEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/cities")
            .WithTags("Cities")
            .RequireRateLimiting("api");

        group.MapGet("/", GetAllCities).AllowAnonymous();
        group.MapPost("/", CreateCity).RequireAuthorization("AdminOnly");
        group.MapDelete("/{cityId:guid}", DeleteCity).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetAllCities(ICityRepository cityRepo, ICacheService cache, CancellationToken ct)     // Returns the list of all cities available for search and scheduling.
    {
        var handler = new GetAllCitiesHandler(cityRepo, cache);
        var cities = await handler.HandleAsync(new GetAllCitiesQuery(), ct);
        return Results.Ok(cities);
    }

    private static async Task<IResult> CreateCity(     // Adds a new city to the system (admin only).
        CreateCityCommand command, ICityRepository cityRepo, ICacheService cache,
        ILogger<CreateCityHandler> logger, CancellationToken ct)
    {
        var handler = new CreateCityHandler(cityRepo, cache, logger);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/cities/{id}", new { cityId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> DeleteCity(     // Removes a city from the system (admin only).
        Guid cityId, ICityRepository cityRepo, ICacheService cache,
        ILogger<DeleteCityHandler> logger, CancellationToken ct)
    {
        var handler = new DeleteCityHandler(cityRepo, cache, logger);
        try
        {
            await handler.HandleAsync(new DeleteCityCommand(cityId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }
}

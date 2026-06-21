using BusBooking.Application.Cities;
using BusBooking.Application.Cities.Commands.CreateCity;
using BusBooking.Application.Cities.Commands.DeleteCity;
using BusBooking.Application.Cities.Queries.GetAllCities;
using BusBooking.Application.Common.Exceptions;

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

    private static async Task<IResult> GetAllCities(ICityRepository cityRepo, CancellationToken ct)
    {
        var handler = new GetAllCitiesHandler(cityRepo);
        var cities = await handler.HandleAsync(new GetAllCitiesQuery(), ct);
        return Results.Ok(cities);
    }

    private static async Task<IResult> CreateCity(
        CreateCityCommand command, ICityRepository cityRepo, CancellationToken ct)
    {
        var handler = new CreateCityHandler(cityRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/cities/{id}", new { cityId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> DeleteCity(
        Guid cityId, ICityRepository cityRepo, CancellationToken ct)
    {
        var handler = new DeleteCityHandler(cityRepo);
        try
        {
            await handler.HandleAsync(new DeleteCityCommand(cityId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }
}

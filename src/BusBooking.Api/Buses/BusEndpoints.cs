using System.Security.Claims;
using BusBooking.Application.Buses;
using BusBooking.Application.Buses.Commands.CreateBus;
using BusBooking.Application.Buses.Commands.DeleteBus;
using BusBooking.Application.Buses.Commands.UpdateBus;
using BusBooking.Application.Buses.Queries.GetVendorBuses;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;

namespace BusBooking.Api.Buses;

public static class BusEndpoints
{
    public static void MapBusEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/buses")
            .WithTags("Buses")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/", CreateBus);
        group.MapPut("/{busId:guid}", UpdateBus);
        group.MapDelete("/{busId:guid}", DeleteBus);
        group.MapGet("/vendor/{vendorId:guid}", GetVendorBuses);
    }

    private static async Task<IResult> CreateBus(
        CreateBusCommand command, IBusRepository busRepo, ITenantContext tenantContext, CancellationToken ct)
    {
        var handler = new CreateBusHandler(busRepo, tenantContext);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/buses/{id}", new { busId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> UpdateBus(
        Guid busId, UpdateBusRequest body, HttpContext httpContext, IBusRepository busRepo, CancellationToken ct)
    {
        if (!GetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var command = new UpdateBusCommand(busId, userId, body.BusName, body.TotalSeats);
        var handler = new UpdateBusHandler(busRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeleteBus(
        Guid busId, HttpContext httpContext, IBusRepository busRepo, CancellationToken ct)
    {
        if (!GetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var handler = new DeleteBusHandler(busRepo);
        try
        {
            await handler.HandleAsync(new DeleteBusCommand(busId, userId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetVendorBuses(
        Guid vendorId, IBusRepository busRepo, CancellationToken ct)
    {
        var handler = new GetVendorBusesHandler(busRepo);
        var buses = await handler.HandleAsync(new GetVendorBusesQuery(vendorId), ct);
        return Results.Ok(buses);
    }

    private static bool GetUserId(HttpContext ctx, out Guid userId) =>
        Guid.TryParse(ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
}

public sealed record UpdateBusRequest(string BusName, int TotalSeats);

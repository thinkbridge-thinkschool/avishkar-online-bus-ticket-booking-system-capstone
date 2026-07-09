using BusBooking.Application.Buses;
using BusBooking.Application.Vendors;
using BusBooking.Application.Buses.Commands.CreateBus;
using BusBooking.Application.Buses.Commands.DeleteBus;
using BusBooking.Application.Buses.Commands.UpdateBus;
using BusBooking.Application.Buses.Queries.GetVendorBuses;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Domain.Scheduling.Enums;

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

        group.MapGet("/mine", GetMyBuses);
        group.MapPost("/", CreateBus);
        group.MapPut("/{busId:guid}", UpdateBus);
        group.MapDelete("/{busId:guid}", DeleteBus);
        group.MapGet("/vendor/{vendorId:guid}", GetVendorBuses);
    }

    private static async Task<IResult> CreateBus(     // Registers a new bus for the authenticated vendor's fleet.
        CreateBusBody body, HttpContext httpContext,
        IVendorRepository vendorRepo, IBusRepository busRepo, ITenantContext tenantContext, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found. Register as a vendor first.");

        var command = new CreateBusCommand(vendor.Id, body.BusNumber, body.BusNumber, body.BusType, body.TotalSeats);
        var handler = new CreateBusHandler(busRepo, tenantContext);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/buses/{id}", new { busId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> UpdateBus(     // Updates the name and seat capacity of a bus owned by the authenticated vendor.
        Guid busId, UpdateBusRequest body, HttpContext httpContext,
        IVendorRepository vendorRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found.");

        var command = new UpdateBusCommand(busId, vendor.Id, body.BusName, body.TotalSeats);
        var handler = new UpdateBusHandler(busRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeleteBus(     // Removes a bus from the authenticated vendor's fleet.
        Guid busId, HttpContext httpContext,
        IVendorRepository vendorRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor profile not found.");

        var handler = new DeleteBusHandler(busRepo);
        try
        {
            await handler.HandleAsync(new DeleteBusCommand(busId, vendor.Id), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> GetVendorBuses(     // Admin Portal: Returns all buses belonging to the specified vendor.
        Guid vendorId, IBusRepository busRepo, CancellationToken ct)
    {
        var handler = new GetVendorBusesHandler(busRepo);
        var buses = await handler.HandleAsync(new GetVendorBusesQuery(vendorId), ct);
        return Results.Ok(buses);
    }

    private static async Task<IResult> GetMyBuses(     // Vendor manage buses: Returns all buses belonging to the authenticated vendor.
        HttpContext httpContext, IVendorRepository vendorRepo, IBusRepository busRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound("Vendor not found.");

        var handler = new GetVendorBusesHandler(busRepo);
        var buses = await handler.HandleAsync(new GetVendorBusesQuery(vendor.Id), ct);
        return Results.Ok(buses);
    }

    private static string? GetAppUserId(HttpContext ctx) =>
        ctx.User.FindFirst("app:userId")?.Value;
}

public sealed record CreateBusBody(string BusNumber, BusType BusType, int TotalSeats);
public sealed record UpdateBusRequest(string BusName, int TotalSeats);

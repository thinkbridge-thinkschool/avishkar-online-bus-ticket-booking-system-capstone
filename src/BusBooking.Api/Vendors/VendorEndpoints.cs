using System.Security.Claims;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Vendors;
using BusBooking.Application.Vendors.Commands.ApproveVendor;
using BusBooking.Application.Vendors.Commands.DeactivateVendor;
using BusBooking.Application.Vendors.Commands.RegisterVendor;
using BusBooking.Application.Vendors.Commands.RejectVendor;
using BusBooking.Application.Vendors.Commands.UpdateVendorProfile;
using BusBooking.Application.Vendors.Queries.GetAllVendors;
using BusBooking.Application.Vendors.Queries.GetPendingVendors;
using BusBooking.Application.Vendors.Queries.GetVendorProfile;

namespace BusBooking.Api.Vendors;

public static class VendorEndpoints
{
    public static void MapVendorEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/vendors")
            .WithTags("Vendors")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/register", RegisterVendor);
        group.MapGet("/{vendorId:guid}", GetVendorProfile);
        group.MapGet("/", GetAllVendors).RequireAuthorization("AdminOnly");
        group.MapGet("/pending", GetPendingVendors).RequireAuthorization("AdminOnly");
        group.MapPut("/{vendorId:guid}", UpdateVendorProfile);
        group.MapPost("/{vendorId:guid}/deactivate", DeactivateVendor);
        group.MapPost("/{vendorId:guid}/approve", ApproveVendor).RequireAuthorization("AdminOnly");
        group.MapPost("/{vendorId:guid}/reject", RejectVendor).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> RegisterVendor(
        RegisterVendorCommand command, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var handler = new RegisterVendorHandler(vendorRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/vendors/{id}", new { vendorId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetVendorProfile(
        Guid vendorId, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var handler = new GetVendorProfileHandler(vendorRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetVendorProfileQuery(vendorId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }

    private static async Task<IResult> GetAllVendors(IVendorRepository vendorRepo, CancellationToken ct)
    {
        var handler = new GetAllVendorsHandler(vendorRepo);
        var vendors = await handler.HandleAsync(new GetAllVendorsQuery(), ct);
        return Results.Ok(vendors);
    }

    private static async Task<IResult> GetPendingVendors(IVendorRepository vendorRepo, CancellationToken ct)
    {
        var handler = new GetPendingVendorsHandler(vendorRepo);
        var vendors = await handler.HandleAsync(new GetPendingVendorsQuery(), ct);
        return Results.Ok(vendors);
    }

    private static async Task<IResult> UpdateVendorProfile(
        Guid vendorId, UpdateVendorProfileRequest body, HttpContext httpContext,
        IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var command = new UpdateVendorProfileCommand(vendorId, oid, body.VendorName, body.PhoneNumber, body.Address);
        var handler = new UpdateVendorProfileHandler(vendorRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeactivateVendor(
        Guid vendorId, HttpContext httpContext, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var handler = new DeactivateVendorHandler(vendorRepo);
        try
        {
            await handler.HandleAsync(new DeactivateVendorCommand(vendorId, oid), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> ApproveVendor(
        Guid vendorId, IVendorRepository vendorRepo, IEventPublisher publisher, CancellationToken ct)
    {
        var handler = new ApproveVendorHandler(vendorRepo, publisher);
        try
        {
            await handler.HandleAsync(new ApproveVendorCommand(vendorId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> RejectVendor(
        Guid vendorId, RejectVendorRequest body,
        IVendorRepository vendorRepo, IEventPublisher publisher, CancellationToken ct)
    {
        var handler = new RejectVendorHandler(vendorRepo, publisher);
        try
        {
            await handler.HandleAsync(new RejectVendorCommand(vendorId, body.Reason), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static string? GetEntraOid(HttpContext ctx) =>
        ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? ctx.User.FindFirst("oid")?.Value;
}

public sealed record UpdateVendorProfileRequest(string VendorName, string PhoneNumber, string Address);
public sealed record RejectVendorRequest(string Reason);

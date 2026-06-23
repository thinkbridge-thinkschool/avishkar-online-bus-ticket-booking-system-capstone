using System.Security.Claims;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Tenants;
using BusBooking.Application.Tenants.Commands.ApproveTenant;
using BusBooking.Application.Tenants.Commands.DeactivateTenant;
using BusBooking.Application.Tenants.Commands.ReactivateTenant;
using BusBooking.Application.Tenants.Commands.RegisterTenant;
using BusBooking.Application.Tenants.Commands.RejectTenant;
using BusBooking.Application.Tenants.Commands.SetRazorpayCredentials;
using BusBooking.Application.Tenants.Commands.SuspendTenant;
using BusBooking.Application.Tenants.Queries.GetAllTenants;
using BusBooking.Application.Tenants.Queries.GetMyTenant;
using BusBooking.Application.Tenants.Queries.GetPendingTenants;
using BusBooking.Application.Tenants.Queries.GetTenantById;

namespace BusBooking.Api.Tenants;

public static class TenantEndpoints
{
    public static void MapTenantEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/tenants")
            .WithTags("Tenants")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        // Vendor Admin — self-service
        group.MapPost("/register", RegisterTenant);
        group.MapGet("/my", GetMyTenant);

        // Super Admin — read
        group.MapGet("/", GetAllTenants).RequireAuthorization("SuperAdminOnly");
        group.MapGet("/pending", GetPendingTenants).RequireAuthorization("SuperAdminOnly");
        group.MapGet("/{tenantId:guid}", GetTenantById).RequireAuthorization("SuperAdminOnly");

        // Super Admin — lifecycle
        group.MapPost("/{tenantId:guid}/approve", ApproveTenant).RequireAuthorization("SuperAdminOnly");
        group.MapPost("/{tenantId:guid}/reject", RejectTenant).RequireAuthorization("SuperAdminOnly");
        group.MapPost("/{tenantId:guid}/suspend", SuspendTenant).RequireAuthorization("SuperAdminOnly");
        group.MapPost("/{tenantId:guid}/reactivate", ReactivateTenant).RequireAuthorization("SuperAdminOnly");
        group.MapPost("/{tenantId:guid}/deactivate", DeactivateTenant).RequireAuthorization("SuperAdminOnly");

        // Super Admin — credentials
        group.MapPut("/{tenantId:guid}/razorpay", SetRazorpayCredentials).RequireAuthorization("SuperAdminOnly");
    }

    private static async Task<IResult> RegisterTenant(
        RegisterTenantRequest body, HttpContext httpContext,
        ITenantRepository tenantRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var email = httpContext.User.FindFirst("preferred_username")?.Value
                 ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                 ?? body.AdminEmail;

        var command = new RegisterTenantCommand(body.Name, body.Subdomain, email, oid);
        var handler = new RegisterTenantHandler(tenantRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/tenants/{id}", new { tenantId = id });
        }
        catch (ArgumentException ex)       { return Results.BadRequest(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetMyTenant(
        HttpContext httpContext, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var handler = new GetMyTenantHandler(tenantRepo);
        var dto = await handler.HandleAsync(new GetMyTenantQuery(oid), ct);
        return dto is null ? Results.NotFound("No tenant registered for this account.") : Results.Ok(dto);
    }

    private static async Task<IResult> GetAllTenants(ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new GetAllTenantsHandler(tenantRepo);
        var tenants = await handler.HandleAsync(new GetAllTenantsQuery(), ct);
        return Results.Ok(tenants);
    }

    private static async Task<IResult> GetPendingTenants(ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new GetPendingTenantsHandler(tenantRepo);
        var tenants = await handler.HandleAsync(new GetPendingTenantsQuery(), ct);
        return Results.Ok(tenants);
    }

    private static async Task<IResult> GetTenantById(
        Guid tenantId, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new GetTenantByIdHandler(tenantRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetTenantByIdQuery(tenantId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }

    private static async Task<IResult> ApproveTenant(
        Guid tenantId, ITenantRepository tenantRepo, IEventPublisher publisher, CancellationToken ct)
    {
        var handler = new ApproveTenantHandler(tenantRepo, publisher);
        try
        {
            await handler.HandleAsync(new ApproveTenantCommand(tenantId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)         { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> RejectTenant(
        Guid tenantId, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new RejectTenantHandler(tenantRepo);
        try
        {
            await handler.HandleAsync(new RejectTenantCommand(tenantId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)         { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> SuspendTenant(
        Guid tenantId, ITenantRepository tenantRepo, IEventPublisher publisher, CancellationToken ct)
    {
        var handler = new SuspendTenantHandler(tenantRepo, publisher);
        try
        {
            await handler.HandleAsync(new SuspendTenantCommand(tenantId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)         { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> ReactivateTenant(
        Guid tenantId, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new ReactivateTenantHandler(tenantRepo);
        try
        {
            await handler.HandleAsync(new ReactivateTenantCommand(tenantId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)         { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> DeactivateTenant(
        Guid tenantId, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new DeactivateTenantHandler(tenantRepo);
        try
        {
            await handler.HandleAsync(new DeactivateTenantCommand(tenantId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)         { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> SetRazorpayCredentials(
        Guid tenantId, SetRazorpayCredentialsRequest body,
        ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new SetRazorpayCredentialsHandler(tenantRepo);
        try
        {
            await handler.HandleAsync(new SetRazorpayCredentialsCommand(tenantId, body.KeyId, body.KeySecret), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex)    { return Results.NotFound(ex.Message); }
        catch (ArgumentException ex)    { return Results.BadRequest(ex.Message); }
    }

    private static string? GetEntraOid(HttpContext ctx) =>
        ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? ctx.User.FindFirst("oid")?.Value;
}

public sealed record RegisterTenantRequest(string Name, string Subdomain, string AdminEmail);
public sealed record SetRazorpayCredentialsRequest(string KeyId, string KeySecret);

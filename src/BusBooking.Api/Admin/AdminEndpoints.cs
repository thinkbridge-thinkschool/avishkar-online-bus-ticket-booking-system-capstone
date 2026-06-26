using BusBooking.Application.Admin.Queries.GetAdminDashboard;
using BusBooking.Application.Admin.Queries.GetTenantMetrics;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Identity;
using BusBooking.Application.Tenants;
using BusBooking.Application.Users;
using BusBooking.Application.Vendors;
using BusBooking.Domain.Identity.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BusBooking.Api.Admin;

public static class AdminEndpoints
{
    private static readonly IReadOnlySet<string> ValidRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        "BusBooking.SuperAdmin",
        "BusBooking.Admin",
        "BusBooking.Vendor"
    };

    public static void MapAdminEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/admin")
            .WithTags("Admin")
            .RequireAuthorization("SuperAdminOnly")
            .RequireRateLimiting("api");

        group.MapGet("/dashboard",           GetDashboard);
        group.MapGet("/tenants/metrics",     GetTenantMetrics);
        group.MapGet("/users",               GetUsers);
        group.MapPost("/users/{id:guid}/roles",                  GrantRole);
        group.MapDelete("/users/{id:guid}/roles/{roleName}",     RevokeRole);
    }

    private static async Task<IResult> GetDashboard(
        IVendorRepository vendorRepo,
        IUserProfileRepository userRepo,
        IBookingRepository bookingRepo,
        ITenantRepository tenantRepo,
        CancellationToken ct)
    {
        var handler = new GetAdminDashboardHandler(vendorRepo, userRepo, bookingRepo, tenantRepo);
        var dto = await handler.HandleAsync(new GetAdminDashboardQuery(), ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> GetTenantMetrics(
        ITenantRepository tenantRepo,
        IBookingRepository bookingRepo,
        CancellationToken ct)
    {
        var handler = new GetTenantMetricsHandler(tenantRepo, bookingRepo);
        var metrics = await handler.HandleAsync(new GetTenantMetricsQuery(), ct);
        return Results.Ok(metrics);
    }

    private static async Task<IResult> GetUsers(
        [FromQuery] int skip,
        [FromQuery] int take,
        IAppUserRepository userRepo,
        CancellationToken ct)
    {
        if (take is < 1 or > 100) take = 50;
        var users = await userRepo.GetAllAsync(skip, take, ct);
        return Results.Ok(users.Select(u => new
        {
            u.Id,
            u.Email,
            u.DisplayName,
            u.EmailVerified,
            providers = u.ExternalLogins.Select(l => l.LoginProvider.ToString()),
            roles     = u.Roles.Select(r => r.RoleName)
        }));
    }

    private static async Task<IResult> GrantRole(
        Guid id,
        GrantRoleRequest body,
        IAppUserRepository userRepo,
        CancellationToken ct)
    {
        if (!ValidRoles.Contains(body.RoleName))
            return Results.BadRequest($"Unknown role. Valid roles: {string.Join(", ", ValidRoles)}");

        var user = await userRepo.GetByIdAsync(id, ct);
        if (user is null) return Results.NotFound();

        if (user.Roles.Any(r => r.RoleName == body.RoleName))
            return Results.Conflict("User already has this role.");

        await userRepo.AddRoleAsync(AppUserRole.Create(id, body.RoleName), ct);
        await userRepo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> RevokeRole(
        Guid id,
        string roleName,
        IAppUserRepository userRepo,
        CancellationToken ct)
    {
        var user = await userRepo.GetByIdAsync(id, ct);
        if (user is null) return Results.NotFound();

        if (!user.Roles.Any(r => r.RoleName == roleName))
            return Results.NotFound("User does not have this role.");

        await userRepo.RemoveRoleAsync(id, roleName, ct);
        await userRepo.SaveChangesAsync(ct);
        return Results.NoContent();
    }
}

public sealed record GrantRoleRequest(string RoleName);

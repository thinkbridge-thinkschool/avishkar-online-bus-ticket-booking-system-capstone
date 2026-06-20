using System.Security.Claims;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Users;
using BusBooking.Application.Users.Commands.CreateUserProfile;
using BusBooking.Application.Users.Commands.DeactivateUser;
using BusBooking.Application.Users.Commands.UpdateUserProfile;
using BusBooking.Application.Users.Queries.GetUserProfile;

namespace BusBooking.Api.Users;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization()
            .RequireRateLimiting("api");

        group.MapPost("/profile", CreateUserProfile);
        group.MapGet("/{userId:guid}/profile", GetUserProfile);
        group.MapPut("/{userId:guid}/profile", UpdateUserProfile);
        group.MapPost("/{userId:guid}/deactivate", DeactivateUser);
    }

    private static async Task<IResult> CreateUserProfile(
        CreateUserProfileCommand command, IUserProfileRepository userRepo, CancellationToken ct)
    {
        var handler = new CreateUserProfileHandler(userRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/users/{id}/profile", new { userId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetUserProfile(
        Guid userId, IUserProfileRepository userRepo, CancellationToken ct)
    {
        var handler = new GetUserProfileHandler(userRepo);
        try
        {
            var dto = await handler.HandleAsync(new GetUserProfileQuery(userId), ct);
            return Results.Ok(dto);
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
    }

    private static async Task<IResult> UpdateUserProfile(
        Guid userId, UpdateUserProfileRequest body, HttpContext httpContext,
        IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var command = new UpdateUserProfileCommand(userId, oid, body.FirstName, body.LastName, body.Phone, body.Address);
        var handler = new UpdateUserProfileHandler(userRepo);
        try
        {
            await handler.HandleAsync(command, ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> DeactivateUser(
        Guid userId, HttpContext httpContext, IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetEntraOid(httpContext);
        if (oid is null) return Results.Unauthorized();

        var handler = new DeactivateUserHandler(userRepo);
        try
        {
            await handler.HandleAsync(new DeactivateUserCommand(userId, oid), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static string? GetEntraOid(HttpContext ctx) =>
        ctx.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? ctx.User.FindFirst("oid")?.Value;
}

public sealed record UpdateUserProfileRequest(
    string FirstName, string LastName, string? Phone, string? Address);

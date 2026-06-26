using System.Security.Claims;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Identity;
using BusBooking.Application.Users;
using BusBooking.Application.Users.Commands.CreateUserProfile;
using BusBooking.Application.Users.Commands.DeactivateUser;
using BusBooking.Application.Users.Commands.UpdateUserProfile;
using BusBooking.Application.Users.Queries.GetUserProfile;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;

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

        group.MapGet("/profile", GetMyProfile);
        group.MapPut("/profile", UpdateMyProfile);
        group.MapPost("/profile", CreateUserProfile);
        group.MapGet("/{userId:guid}/profile", GetUserProfile);
        group.MapPut("/{userId:guid}/profile", UpdateUserProfile);
        group.MapPost("/{userId:guid}/deactivate", DeactivateUser);

        // Account linking
        group.MapGet("/me/linked-accounts",             GetLinkedAccounts);
        group.MapPost("/me/link-local",                 LinkLocal);
        group.MapDelete("/me/linked-accounts/{provider}", UnlinkProvider);
    }

    // ── GET /profile ──────────────────────────────────────────────────────────
    private static async Task<IResult> GetMyProfile(
        HttpContext httpContext, IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var profile = await userRepo.GetByEntraObjectIdAsync(oid, ct);
        if (profile is null) return Results.NotFound();

        var fullName = $"{profile.FirstName} {profile.LastName}".Trim();
        return Results.Ok(new MyProfileResponse(
            profile.Id.ToString(), profile.Email, fullName, profile.Phone, profile.Address));
    }

    // ── PUT /profile ──────────────────────────────────────────────────────────
    private static async Task<IResult> UpdateMyProfile(
        UpdateMyProfileRequest body, HttpContext httpContext,
        IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(body.FullName))
            return Results.BadRequest("Full name is required.");

        SplitFullName(body.FullName, out var firstName, out var lastName);

        var profile = await userRepo.GetByEntraObjectIdAsync(oid, ct);
        if (profile is not null)
        {
            profile.Update(firstName, lastName, body.PhoneNumber, body.Address);
            await userRepo.SaveChangesAsync(ct);
            return Results.NoContent();
        }

        // First-time save — create the profile
        var email = httpContext.User.FindFirst("email")?.Value
                 ?? httpContext.User.FindFirst("preferred_username")?.Value
                 ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value
                 ?? "";

        var createCommand = new CreateUserProfileCommand(oid, firstName, lastName, email);
        var createHandler = new CreateUserProfileHandler(userRepo);
        try
        {
            await createHandler.HandleAsync(createCommand, ct);
            return Results.NoContent();
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    // ── POST /profile (admin / programmatic) ─────────────────────────────────
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

    // ── GET /{userId:guid}/profile ────────────────────────────────────────────
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

    // ── PUT /{userId:guid}/profile ────────────────────────────────────────────
    private static async Task<IResult> UpdateUserProfile(
        Guid userId, UpdateUserProfileRequest body, HttpContext httpContext,
        IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
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

    // ── POST /{userId:guid}/deactivate ────────────────────────────────────────
    private static async Task<IResult> DeactivateUser(
        Guid userId, HttpContext httpContext, IUserProfileRepository userRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
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

    // ── GET /me/linked-accounts ───────────────────────────────────────────────
    private static async Task<IResult> GetLinkedAccounts(
        HttpContext ctx,
        IAppUserRepository userRepo,
        CancellationToken ct)
    {
        var oidStr = GetAppUserId(ctx);
        if (oidStr is null) return Results.Unauthorized();
        if (!Guid.TryParse(oidStr, out var userId)) return Results.Unauthorized();

        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        return Results.Ok(user.ExternalLogins.Select(l => new
        {
            provider   = l.LoginProvider.ToString(),
            linkedAt   = l.CreatedAt
        }));
    }

    // ── POST /me/link-local ───────────────────────────────────────────────────
    // Allows an MSAL-only account to add a local email/password credential.
    private static async Task<IResult> LinkLocal(
        LinkLocalRequest body,
        HttpContext ctx,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IPasswordService passwords,
        CancellationToken ct)
    {
        if (body.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters.");

        var oidStr = GetAppUserId(ctx);
        if (oidStr is null) return Results.Unauthorized();
        if (!Guid.TryParse(oidStr, out var userId)) return Results.Unauthorized();

        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        var existing = await credRepo.GetByAppUserIdAsync(userId, ct);
        if (existing is not null)
            return Results.Conflict("Local credentials already linked to this account.");

        // Trust the email from AppUser (populated by MSAL / AppClaimsTransformer)
        var hasLocalLogin = user.ExternalLogins.Any(l => l.LoginProvider == LoginProvider.Local);
        if (!hasLocalLogin)
        {
            var login = ExternalLogin.Create(userId, LoginProvider.Local, user.Email);
            await userRepo.AddExternalLoginAsync(login, ct);
        }

        var credential = LocalCredential.Create(userId, passwords.Hash(body.Password));
        await credRepo.AddAsync(credential, ct);

        // Mark email as verified — identity already confirmed via MSAL
        if (!user.EmailVerified) user.VerifyEmail();

        await credRepo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    // ── DELETE /me/linked-accounts/{provider} ─────────────────────────────────
    private static async Task<IResult> UnlinkProvider(
        string provider,
        HttpContext ctx,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IRefreshTokenRepository refreshRepo,
        CancellationToken ct)
    {
        if (!Enum.TryParse<LoginProvider>(provider, ignoreCase: true, out var loginProvider))
            return Results.BadRequest($"Unknown provider '{provider}'. Valid values: Entra, Local.");

        var oidStr = GetAppUserId(ctx);
        if (oidStr is null) return Results.Unauthorized();
        if (!Guid.TryParse(oidStr, out var userId)) return Results.Unauthorized();

        var user = await userRepo.GetByIdAsync(userId, ct);
        if (user is null) return Results.NotFound();

        if (user.ExternalLogins.Count <= 1)
            return Results.BadRequest("Cannot unlink the only sign-in method on this account.");

        var login = user.ExternalLogins.FirstOrDefault(l => l.LoginProvider == loginProvider);
        if (login is null)
            return Results.NotFound($"Provider '{provider}' is not linked to this account.");

        // Removing local: also remove the local credential and all active sessions
        if (loginProvider == LoginProvider.Local)
        {
            await credRepo.RemoveAsync(userId, ct);
            await refreshRepo.RevokeAllForUserAsync(userId, ct);
        }

        await userRepo.RemoveExternalLoginAsync(userId, loginProvider, ct);
        await userRepo.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static string? GetAppUserId(HttpContext ctx) =>
        ctx.User.FindFirst("app:userId")?.Value;

    private static void SplitFullName(string fullName, out string firstName, out string lastName)
    {
        var trimmed = fullName.Trim();
        var idx = trimmed.IndexOf(' ');
        firstName = idx > 0 ? trimmed[..idx] : trimmed;
        lastName  = idx > 0 ? trimmed[(idx + 1)..] : trimmed;
    }
}

public sealed record MyProfileResponse(
    string UserId, string Email, string FullName, string? PhoneNumber, string? Address);

public sealed record UpdateMyProfileRequest(
    string FullName, string? PhoneNumber, string? Address);

public sealed record UpdateUserProfileRequest(
    string FirstName, string LastName, string? Phone, string? Address);

public sealed record LinkLocalRequest(string Password);

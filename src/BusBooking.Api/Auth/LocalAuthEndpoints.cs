using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Identity;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace BusBooking.Api.Auth;

public static class LocalAuthEndpoints
{
    private const string RefreshTokenCookie  = "rt";
    private const int    MaxFailedAttempts   = 10;
    private const int    LockDurationMinutes = 15;
    private const int    VerificationTokenExpiryHours = 24;
    private const int    ResetTokenExpiryHours = 2;

    public static void MapLocalAuthEndpoints(this WebApplication app)
    {
        var group = app
            .MapGroup("/api/v1/auth")
            .WithTags("LocalAuth")
            .AllowAnonymous()
            .RequireRateLimiting("auth"); // 10/min per IP for most auth endpoints

        // Sensitive endpoints get a stricter 5/min per IP limit
        group.MapPost("/register",        Register);
        group.MapPost("/login",           Login)         .RequireRateLimiting("auth-strict");
        group.MapPost("/refresh",         Refresh);
        group.MapPost("/logout",          Logout);
        group.MapGet( "/verify-email",    VerifyEmail);
        group.MapPost("/forgot-password", ForgotPassword).RequireRateLimiting("auth-strict");
        group.MapPost("/reset-password",  ResetPassword) .RequireRateLimiting("auth-strict");
    }

    // ── POST /api/v1/auth/register ─────────────────────────────────────────
    private static async Task<IResult> Register(
        RegisterRequest body,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IPasswordService passwords,
        IEmailService email,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        if (!IsValidEmail(body.Email))
            return Results.BadRequest("Invalid email address.");
        if (string.IsNullOrWhiteSpace(body.DisplayName))
            return Results.BadRequest("Display name is required.");
        if (body.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters.");

        var existing = await userRepo.GetByEmailAsync(body.Email.ToLowerInvariant(), ct); // checks for already email present
        if (existing is not null)
            return Results.Conflict("An account with this email already exists.");

        var appUserId    = Guid.NewGuid();
        var user         = AppUser.Create(appUserId, body.Email.ToLowerInvariant(), body.DisplayName);
        var login        = ExternalLogin.Create(appUserId, LoginProvider.Local, body.Email.ToLowerInvariant());
        var passwordHash = passwords.Hash(body.Password);
        var credential   = LocalCredential.Create(appUserId, passwordHash);

        var (rawToken, tokenHash) = GenerateToken();
        credential.SetEmailVerificationToken(tokenHash,
            DateTime.UtcNow.AddHours(VerificationTokenExpiryHours));

        await userRepo.AddAsync(user, ct); // It only adds the object to EF Core's Change Tracker.
        await userRepo.AddExternalLoginAsync(login, ct); // Think of it like writing items on a shopping list.
        await credRepo.AddAsync(credential, ct); // Add the credential to the Change Tracker.
        await credRepo.SaveChangesAsync(ct); // Persist all changes to the database.

        var verifyUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/v1/auth/verify-email?token={rawToken}";
        await email.SendEmailVerificationAsync(user.Email, user.DisplayName, verifyUrl, ct);

        await audit.AddAsync(AuthAuditLog.Create(
            AuthAuditLog.Events.Register, success: true,
            email: user.Email, appUserId: appUserId,
            ipAddress: GetClientIp(ctx), userAgent: GetUserAgent(ctx)), ct);
        await audit.SaveChangesAsync(ct);

        return Results.Created($"/api/v1/users/{appUserId}", new
        {
            message = "Registration successful. Check your email to verify your account.",
            userId  = appUserId
        });
    }

    // ── POST /api/v1/auth/login ────────────────────────────────────────────
    private static async Task<IResult> Login(
        LoginRequest body,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IRefreshTokenRepository refreshRepo,
        IPasswordService passwords,
        IJwtTokenService jwt,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        var ip        = GetClientIp(ctx);
        var userAgent = GetUserAgent(ctx);

        var user = await userRepo.GetByEmailAsync(body.Email.ToLowerInvariant(), ct);
        if (user is null)
        {
            await RecordAudit(audit, AuthAuditLog.Events.LoginFailure, false, body.Email, null, ip, userAgent, ct);
            return Results.Unauthorized();
        }

        var credential = await credRepo.GetByAppUserIdAsync(user.Id, ct); // contains password-specific information such as the BCrypt password hash, failed login attempts, and verification tokens
        if (credential is null)
        {
            await RecordAudit(audit, AuthAuditLog.Events.LoginFailure, false, body.Email, user.Id, ip, userAgent, ct);
            return Results.Unauthorized(); // MSAL-only account
        }

        if (credential.IsLocked()) // Is this account currently locked, multile failed login attempts have been made in a short period of time, so the account is temporarily locked to prevent brute-force attacks.
        {
            await RecordAudit(audit, AuthAuditLog.Events.LoginLocked, false, body.Email, user.Id, ip, userAgent, ct);
            return Results.Problem("Account is temporarily locked due to too many failed attempts.",
                statusCode: StatusCodes.Status423Locked);
        }

        if (!passwords.Verify(body.Password, credential.PasswordHash))
        {
            credential.RecordFailedLogin(MaxFailedAttempts, TimeSpan.FromMinutes(LockDurationMinutes));
            await credRepo.SaveChangesAsync(ct);
            await RecordAudit(audit, AuthAuditLog.Events.LoginFailure, false, body.Email, user.Id, ip, userAgent, ct);
            return Results.Unauthorized();
        }

        if (!user.EmailVerified)
            return Results.Problem("Email address has not been verified. Check your inbox for the verification link.",
                statusCode: StatusCodes.Status403Forbidden);

        credential.RecordSuccessfulLogin();   // FailedLoginAttempts = 0;
        await credRepo.SaveChangesAsync(ct);

        var roles       = user.Roles.Select(r => r.RoleName);  // Collect User Roles to include in the JWT token. This allows the application to know what permissions the user has without needing to query the database again.
        var accessToken = jwt.IssueAccessToken(user.Id, user.Email, user.DisplayName, roles); // Issue a JWT access token for the user, which will be used for authenticating subsequent requests to the API.

        var (rawRefresh, refreshHash) = GenerateToken(); // Generate a new refresh token, which will be used to obtain new access tokens without requiring the user to log in again.
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var refresh   = RefreshToken.Create(user.Id, refreshHash, expiresAt);
        await refreshRepo.AddAsync(refresh, ct);
        await refreshRepo.SaveChangesAsync(ct);

        SetRefreshCookie(ctx, rawRefresh, expiresAt);

        await RecordAudit(audit, AuthAuditLog.Events.LoginSuccess, true, body.Email, user.Id, ip, userAgent, ct);

        return Results.Ok(new
        {
            accessToken,
            expiresIn = jwt.AccessTokenExpiryMinutes * 60,
            tokenType = "Bearer"
        });
    }

    // ── POST /api/v1/auth/refresh ──────────────────────────────────────────
    private static async Task<IResult> Refresh(
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IRefreshTokenRepository refreshRepo,
        IJwtTokenService jwt,
        IAuthAuditLogRepository audit,
        ILoggerFactory loggerFactory,
        HttpContext ctx,
        CancellationToken ct)
    {
        var rawToken = ctx.Request.Cookies[RefreshTokenCookie];
        if (string.IsNullOrEmpty(rawToken)) return Results.Unauthorized();

        var tokenHash  = HashToken(rawToken);
        var stored     = await refreshRepo.GetByTokenHashAsync(tokenHash, ct);
        if (stored is null) return Results.Unauthorized();

        if (!stored.IsActive)
        {
            // Token reuse detected — revoke entire family and alert via log
            await refreshRepo.RevokeAllForUserAsync(stored.AppUserId, ct);
            await refreshRepo.SaveChangesAsync(ct);
            ClearRefreshCookie(ctx);

            loggerFactory.CreateLogger("BusBooking.Auth").LogWarning(
                "Refresh token reuse detected for user {UserId} from IP {Ip} — all sessions revoked.",
                stored.AppUserId, GetClientIp(ctx));

            await RecordAudit(audit, AuthAuditLog.Events.TokenReuse, false,
                email: "", appUserId: stored.AppUserId,
                ip: GetClientIp(ctx), userAgent: GetUserAgent(ctx), ct);

            return Results.Unauthorized();
        }

        var user = await userRepo.GetByIdAsync(stored.AppUserId, ct);
        if (user is null) return Results.Unauthorized();

        var (rawNew, hashNew) = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddDays(7);
        var newToken  = RefreshToken.Create(user.Id, hashNew, expiresAt);

        stored.Revoke(newToken.Id);
        await refreshRepo.AddAsync(newToken, ct);
        await refreshRepo.SaveChangesAsync(ct);

        var roles       = user.Roles.Select(r => r.RoleName);
        var accessToken = jwt.IssueAccessToken(user.Id, user.Email, user.DisplayName, roles);
        SetRefreshCookie(ctx, rawNew, expiresAt);

        return Results.Ok(new
        {
            accessToken,
            expiresIn = jwt.AccessTokenExpiryMinutes * 60,
            tokenType = "Bearer"
        });
    }

    // ── POST /api/v1/auth/logout ───────────────────────────────────────────
    private static async Task<IResult> Logout(
        IRefreshTokenRepository refreshRepo,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        var rawToken = ctx.Request.Cookies[RefreshTokenCookie];
        Guid? userId = null;
        if (!string.IsNullOrEmpty(rawToken))
        {
            var stored = await refreshRepo.GetByTokenHashAsync(HashToken(rawToken), ct);
            if (stored is not null && stored.IsActive)
            {
                userId = stored.AppUserId;
                stored.Revoke();
                await refreshRepo.SaveChangesAsync(ct);
            }
        }
        ClearRefreshCookie(ctx);

        await RecordAudit(audit, AuthAuditLog.Events.Logout, true,
            email: "", appUserId: userId,
            ip: GetClientIp(ctx), userAgent: GetUserAgent(ctx), ct);

        return Results.NoContent();
    }

    // ── GET /api/v1/auth/verify-email?token=xxx ────────────────────────────
    private static async Task<IResult> VerifyEmail(
        [FromQuery] string token,
        ILocalCredentialRepository credRepo,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        var tokenHash  = HashToken(token);
        var credential = await credRepo.GetByEmailVerificationTokenAsync(tokenHash, ct);
        if (credential is null)
            return Results.BadRequest("Verification link is invalid or has expired.");

        credential.AppUser.VerifyEmail();
        credential.ClearEmailVerificationToken(); // The same link could be used
        await credRepo.SaveChangesAsync(ct);

        await RecordAudit(audit, AuthAuditLog.Events.EmailVerified, true,
            email: credential.AppUser.Email, appUserId: credential.AppUserId,
            ip: GetClientIp(ctx), userAgent: GetUserAgent(ctx), ct);

        return Results.Ok(new { message = "Email verified successfully. You can now log in." });
    }

    // ── POST /api/v1/auth/forgot-password ─────────────────────────────────
    private static async Task<IResult> ForgotPassword(
        ForgotPasswordRequest body,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IEmailService email,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        // Always return 200 to prevent user enumeration
        var user = await userRepo.GetByEmailAsync(body.Email.ToLowerInvariant(), ct);
        if (user is not null)
        {
            var credential = await credRepo.GetByAppUserIdAsync(user.Id, ct);
            if (credential is not null)
            {
                var (rawToken, tokenHash) = GenerateToken();
                credential.SetPasswordResetToken(tokenHash,
                    DateTime.UtcNow.AddHours(ResetTokenExpiryHours));
                await credRepo.SaveChangesAsync(ct);

                var resetUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}" +
                               $"/reset-password?token={rawToken}";
                await email.SendPasswordResetAsync(user.Email, user.DisplayName, resetUrl, ct);

                await RecordAudit(audit, AuthAuditLog.Events.ForgotPassword, true,
                    email: user.Email, appUserId: user.Id,
                    ip: GetClientIp(ctx), userAgent: GetUserAgent(ctx), ct);
            }
        }

        return Results.Ok(new
        {
            message = "If an account with that email exists, a reset link has been sent."
        });
    }

    // ── POST /api/v1/auth/reset-password ──────────────────────────────────
    private static async Task<IResult> ResetPassword(
        ResetPasswordRequest body,
        ILocalCredentialRepository credRepo,
        IRefreshTokenRepository refreshRepo,
        IPasswordService passwords,
        IAuthAuditLogRepository audit,
        HttpContext ctx,
        CancellationToken ct)
    {
        if (body.NewPassword.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters.");

        var tokenHash  = HashToken(body.Token);
        var credential = await credRepo.GetByPasswordResetTokenAsync(tokenHash, ct);
        if (credential is null)
            return Results.BadRequest("Reset link is invalid or has expired.");

        credential.UpdatePasswordHash(passwords.Hash(body.NewPassword));
        credential.ClearPasswordResetToken();

        // Invalidate all active sessions after a password change
        await refreshRepo.RevokeAllForUserAsync(credential.AppUserId, ct);
        await refreshRepo.SaveChangesAsync(ct);

        await RecordAudit(audit, AuthAuditLog.Events.PasswordReset, true,
            email: "", appUserId: credential.AppUserId,
            ip: GetClientIp(ctx), userAgent: GetUserAgent(ctx), ct);

        return Results.Ok(new { message = "Password reset successfully. You can now log in." });
    }

    // ── Cookie helpers ─────────────────────────────────────────────────────

    private static void SetRefreshCookie(HttpContext ctx, string rawToken, DateTime expiresAt)
    {
        ctx.Response.Cookies.Append(RefreshTokenCookie, rawToken, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Expires   = expiresAt,
            Path      = "/api/v1/auth"  // restrict to auth routes only
        });
    }

    private static void ClearRefreshCookie(HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(RefreshTokenCookie, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Path      = "/api/v1/auth"
        });
    }

    // ── Token utilities ────────────────────────────────────────────────────

    private static (string raw, string hash) GenerateToken()
    {
        var raw  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                       .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        return (raw, HashToken(raw));
    }

    private static string HashToken(string raw)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        email.Contains('@') &&
        email.IndexOf('@') > 0 &&
        email.IndexOf('@') < email.Length - 2;

    private static string? GetClientIp(HttpContext ctx) =>
        ctx.Connection.RemoteIpAddress?.ToString();

    private static string? GetUserAgent(HttpContext ctx) =>
        ctx.Request.Headers.UserAgent.FirstOrDefault();

    private static async Task RecordAudit(
        IAuthAuditLogRepository audit,
        string eventType, bool success,
        string email, Guid? appUserId,
        string? ip, string? userAgent,
        CancellationToken ct)
    {
        try
        {
            await audit.AddAsync(AuthAuditLog.Create(eventType, success, email, appUserId, ip, userAgent), ct);
            await audit.SaveChangesAsync(ct);
        }
        catch
        {
            // Audit failures must never surface as 5xx to the caller
        }
    }
}

// ── Request/response records ───────────────────────────────────────────────
public sealed record RegisterRequest(string Email, string Password, string DisplayName);
public sealed record LoginRequest(string Email, string Password);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword);

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BusBooking.Application.Common;
using BusBooking.Application.Common.Exceptions;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Identity;
using BusBooking.Application.Tenants;
using BusBooking.Application.Vendors;
using BusBooking.Application.Vendors.Commands.AdminCreateVendor;
using BusBooking.Application.Vendors.Commands.ApproveVendor;
using BusBooking.Application.Vendors.Commands.DeactivateVendor;
using BusBooking.Application.Vendors.Commands.RegisterVendor;
using BusBooking.Application.Vendors.Commands.RejectVendor;
using BusBooking.Application.Vendors.Commands.UpdateVendorProfile;
using BusBooking.Application.Vendors.Queries.GetAllVendors;
using BusBooking.Application.Vendors.Queries.GetPendingVendors;
using BusBooking.Application.Vendors.Queries.GetVendorProfile;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;
using BusBooking.Domain.Vendor.Aggregates;

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
        group.MapPost("/register-new", RegisterNewVendorAccount).AllowAnonymous().RequireRateLimiting("auth");
        group.MapPost("/admin-create", AdminCreateVendor).RequireAuthorization("AdminOnly");
        group.MapGet("/me", GetMyVendorProfile);
        group.MapGet("/{vendorId:guid}", GetVendorProfile);
        group.MapGet("/", GetAllVendors).RequireAuthorization("AdminOnly");
        group.MapGet("/pending", GetPendingVendors).RequireAuthorization("AdminOnly");
        group.MapPut("/{vendorId:guid}", UpdateVendorProfile);
        group.MapPost("/{vendorId:guid}/deactivate", DeactivateVendor);
        group.MapPost("/{vendorId:guid}/approve", ApproveVendor).RequireAuthorization("AdminOnly");
        group.MapPost("/{vendorId:guid}/reject", RejectVendor).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> RegisterVendor(     // Attaches a vendor profile to the authenticated, already signed-in user.
        HttpContext httpContext, RegisterVendorBody body, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var command = new RegisterVendorCommand(oid, body.VendorName, body.Email,
            body.PhoneNumber, body.Address, body.LicenseNumber);
        var handler = new RegisterVendorHandler(vendorRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/vendors/{id}", new { vendorId = id });
        }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    // Public, unauthenticated self-service signup: creates a brand-new local account
    // (mirrors LocalAuthEndpoints.Register) *and* a Pending vendor profile for it in one
    // step, for people who don't already have a BusBooking account. Contrast with
    // RegisterVendor above, which attaches a vendor profile to an *existing*, already
    // signed-in user — this is the only vendor entry point that also creates the login.
    private static async Task<IResult> RegisterNewVendorAccount(     // Self-service signup that creates a new local account and a pending vendor profile in one step.
        RegisterNewVendorBody body,
        IAppUserRepository userRepo,
        ILocalCredentialRepository credRepo,
        IPasswordService passwords,
        IEmailService email,
        IVendorRepository vendorRepo,
        HttpContext ctx,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.Email) || !body.Email.Contains('@'))
            return Results.BadRequest("A valid email address is required.");
        if (string.IsNullOrWhiteSpace(body.VendorName))
            return Results.BadRequest("Vendor name is required.");
        if (body.Password.Length < 8)
            return Results.BadRequest("Password must be at least 8 characters.");
        if (body.Password != body.ConfirmPassword)
            return Results.BadRequest("Passwords do not match.");

        var normalizedEmail = body.Email.Trim().ToLowerInvariant();

        if (await userRepo.GetByEmailAsync(normalizedEmail, ct) is not null)
            return Results.Conflict("An account with this email already exists.");
        if (await vendorRepo.GetByEmailAsync(normalizedEmail, ct) is not null)
            return Results.Conflict($"A vendor with email '{normalizedEmail}' is already registered.");

        var appUserId    = Guid.NewGuid();
        var user         = AppUser.Create(appUserId, normalizedEmail, body.VendorName);
        var login        = ExternalLogin.Create(appUserId, LoginProvider.Local, normalizedEmail);
        var passwordHash = passwords.Hash(body.Password);
        var credential   = LocalCredential.Create(appUserId, passwordHash);

        var rawToken  = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        credential.SetEmailVerificationToken(tokenHash, DateTime.UtcNow.AddHours(24));

        await userRepo.AddAsync(user, ct);
        await userRepo.AddExternalLoginAsync(login, ct);
        await credRepo.AddAsync(credential, ct);
        await credRepo.SaveChangesAsync(ct);

        var vendor = Vendor.Register(
            appUserId.ToString(), body.VendorName, normalizedEmail, body.PhoneNumber, body.Address, body.LicenseNumber);
        await vendorRepo.AddAsync(vendor, ct);
        await vendorRepo.SaveChangesAsync(ct);

        var verifyUrl = $"{ctx.Request.Scheme}://{ctx.Request.Host}/api/v1/auth/verify-email?token={rawToken}";
        await email.SendEmailVerificationAsync(user.Email, user.DisplayName, verifyUrl, ct);

        return Results.Created($"/api/v1/vendors/{vendor.Id}", new
        {
            message = "Registration submitted! Check your email to verify your account, " +
                      "then wait for admin approval before you can sign in.",
            vendorId = vendor.Id
        });
    }

    private static async Task<IResult> AdminCreateVendor(     // Creates a vendor profile and account on behalf of a user (admin only).
        AdminCreateVendorBody body, IVendorRepository vendorRepo, IAppUserRepository userRepo,
        ITenantRepository tenantRepo, CancellationToken ct)
    {
        var command = new AdminCreateVendorCommand(
            body.UserEmail, body.VendorName, body.PhoneNumber, body.Address, body.LicenseNumber);
        var handler = new AdminCreateVendorHandler(vendorRepo, userRepo, tenantRepo);
        try
        {
            var id = await handler.HandleAsync(command, ct);
            return Results.Created($"/api/v1/vendors/{id}", new { vendorId = id });
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> GetMyVendorProfile(     // Vendor Portal: Returns the vendor profile of the authenticated user.
        HttpContext httpContext, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var vendor = await vendorRepo.GetByEntraObjectIdAsync(oid, ct);
        if (vendor is null) return Results.NotFound();

        return Results.Ok(new
        {
            vendorId    = vendor.Id,
            vendorName  = vendor.VendorName,
            email       = vendor.Email,
            phoneNumber = vendor.PhoneNumber,
            address     = vendor.Address,
            licenseNumber = vendor.LicenseNumber,
            status      = vendor.Status.ToString(),
            isActive    = vendor.IsActive,
        });
    }

    private static async Task<IResult> GetVendorProfile(     // Admin Portal: Returns the vendor profile for the specified vendor.
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

    private static async Task<IResult> GetAllVendors(IVendorRepository vendorRepo, CancellationToken ct)     // Admin Portal: Returns all vendors in the system (admin only).
    {
        var handler = new GetAllVendorsHandler(vendorRepo);
        var vendors = await handler.HandleAsync(new GetAllVendorsQuery(), ct);
        return Results.Ok(vendors);
    }

    private static async Task<IResult> GetPendingVendors(IVendorRepository vendorRepo, CancellationToken ct)     // Admin Portal: Returns vendors awaiting approval (admin only).
    {
        var handler = new GetPendingVendorsHandler(vendorRepo);
        var vendors = await handler.HandleAsync(new GetPendingVendorsQuery(), ct);
        return Results.Ok(vendors);
    }

    private static async Task<IResult> UpdateVendorProfile(     // Vendor Portal: Updates the profile of a vendor owned by the authenticated user.
        Guid vendorId, UpdateVendorProfileRequest body, HttpContext httpContext,
        IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
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

    private static async Task<IResult> DeactivateVendor(     // Deactivates a vendor profile, restricted to its owner or an admin.
        Guid vendorId, HttpContext httpContext, IVendorRepository vendorRepo, CancellationToken ct)
    {
        var oid = GetAppUserId(httpContext);
        if (oid is null) return Results.Unauthorized();

        var isAdmin = httpContext.User.IsInRole("BusBooking.SuperAdmin");
        var handler = new DeactivateVendorHandler(vendorRepo);
        try
        {
            await handler.HandleAsync(new DeactivateVendorCommand(vendorId, oid, isAdmin), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (UnauthorizedAccessException) { return Results.Forbid(); }
    }

    private static async Task<IResult> ApproveVendor(     // Approves a pending vendor, activating their account (admin only).
        Guid vendorId, IVendorRepository vendorRepo, IAppUserRepository userRepo,
        IEventPublisher publisher, ITenantRepository tenantRepo, CancellationToken ct)
    {
        var handler = new ApproveVendorHandler(vendorRepo, userRepo, publisher, tenantRepo);
        try
        {
            await handler.HandleAsync(new ApproveVendorCommand(vendorId), ct);
            return Results.NoContent();
        }
        catch (NotFoundException ex) { return Results.NotFound(ex.Message); }
        catch (InvalidOperationException ex) { return Results.Conflict(ex.Message); }
    }

    private static async Task<IResult> RejectVendor(     // Rejects a pending vendor registration with a reason (admin only).
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

    private static string? GetAppUserId(HttpContext ctx) =>
        ctx.User.FindFirst("app:userId")?.Value;
}

public sealed record RegisterVendorBody(
    string VendorName, string Email, string PhoneNumber, string Address, string LicenseNumber);
public sealed record RegisterNewVendorBody(
    string VendorName, string Email, string PhoneNumber, string Password, string ConfirmPassword,
    string Address, string LicenseNumber);
public sealed record AdminCreateVendorBody(
    string UserEmail, string VendorName, string PhoneNumber, string Address, string LicenseNumber);
public sealed record UpdateVendorProfileRequest(string VendorName, string PhoneNumber, string Address);
public sealed record RejectVendorRequest(string Reason);

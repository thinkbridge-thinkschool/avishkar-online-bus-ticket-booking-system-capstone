using BusBooking.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace BusBooking.Api.IntegrationTests.Auth;

/// <summary>
/// Inherits ApiFactory (in-memory DB, no background services, LocalJwt config included).
/// Raises auth rate limits so 12 sequential test requests don't exhaust the per-IP window.
/// </summary>
public sealed class LocalAuthApiFactory : ApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // High limits so test requests never hit the ceiling
                ["RateLimits:AuthStrictPerMinute"] = "500",
                ["RateLimits:AuthPerMinute"]       = "500",
            });
        });
        base.ConfigureWebHost(builder);
    }
}

public sealed class LocalAuthIntegrationTests : IClassFixture<LocalAuthApiFactory>
{
    private readonly LocalAuthApiFactory _factory;

    public LocalAuthIntegrationTests(LocalAuthApiFactory factory) => _factory = factory;

    // ── helpers ───────────────────────────────────────────────────────────────

    private HttpClient Client() => _factory.CreateClient(new() { AllowAutoRedirect = false });

    private static string UniqueEmail() => $"user.{Guid.NewGuid():N}@test.local";

    private async Task RegisterAsync(HttpClient client, string email,
        string password = "ValidPass1!", string name = "Test User")
    {
        var res = await client.PostAsJsonAsync("/api/v1/auth/register",
            new { email, password, displayName = name });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    private void VerifyEmailInDb(string email)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();
        var user = db.AppUsers.First(u => u.Email == email);
        user.VerifyEmail();
        db.SaveChanges();
    }

    // ── POST /api/v1/auth/register ────────────────────────────────────────────

    [Fact]
    public async Task Register_WithValidData_Returns201()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email       = UniqueEmail(),
            password    = "ValidPass1!",
            displayName = "New User",
        });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var client = Client();
        var email  = UniqueEmail();
        await RegisterAsync(client, email);

        var second = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password    = "AnotherPass1!",
            displayName = "Duplicate",
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email       = "not-an-email",
            password    = "ValidPass1!",
            displayName = "Bad Email User",
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_ShortPassword_Returns400()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email       = UniqueEmail(),
            password    = "short",
            displayName = "Short Pwd",
        });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ── POST /api/v1/auth/login ───────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email    = "nobody@test.local",
            password = "SomePass1!",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithoutEmailVerification_Returns403()
    {
        var client = Client();
        var email  = UniqueEmail();
        await RegisterAsync(client, email);

        // Email is NOT verified — expect 403
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "ValidPass1!",
        });
        Assert.Equal(HttpStatusCode.Forbidden, loginRes.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = Client();
        var email  = UniqueEmail();
        await RegisterAsync(client, email);
        VerifyEmailInDb(email);

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "WrongPassword99!",
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginRes.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Returns200WithAccessToken()
    {
        var client = Client();
        var email  = UniqueEmail();
        await RegisterAsync(client, email);
        VerifyEmailInDb(email);

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password = "ValidPass1!",
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var body = await loginRes.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(body);
        Assert.True(body.ContainsKey("accessToken"));
    }

    // ── POST /api/v1/auth/logout ──────────────────────────────────────────────

    [Fact]
    public async Task Logout_WithoutCookie_Returns204()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
    }

    // ── POST /api/v1/auth/forgot-password ─────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_WithKnownEmail_Returns200()
    {
        var client = Client();
        var email  = UniqueEmail();
        await RegisterAsync(client, email);

        var res = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_StillReturns200()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/forgot-password",
            new { email = "nobody@test.local" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    // ── POST /api/v1/auth/refresh ─────────────────────────────────────────────

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        var client = Client();
        var res = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}

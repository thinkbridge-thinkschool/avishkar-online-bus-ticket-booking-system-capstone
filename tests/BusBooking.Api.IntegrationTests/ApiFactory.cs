using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusBooking.Api.IntegrationTests;

/// <summary>
/// Factory that boots the API against an in-memory SQLite provider with a
/// fake JWT handler so tests can run without Entra ID.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureServices(services =>
        {
            // ── Replace SQL Server with in-memory ──────────────────────────────
            // Remove the DbContext registration AND every options configuration
            // descriptor so the SqlServer provider is not present alongside InMemory.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<BusBookingDbContext>)
                         || d.ServiceType.FullName?.StartsWith("Microsoft.EntityFrameworkCore") == true
                             && d.ServiceType.Name.Contains("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<BusBookingDbContext>(opts =>
                opts.UseInMemoryDatabase("integration_tests"));

            // ── Replace JWT bearer with test header auth ───────────────────────
            services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // ── Disable background services (seat expiry etc.) ─────────────────
            services.RemoveAll<IHostedService>();

            // ── No Service Bus in tests ────────────────────────────────────────
            services.RemoveAll<BusBooking.Application.Common.IEventPublisher>();
            services.AddScoped<BusBooking.Application.Common.IEventPublisher,
                               BusBooking.Api.NoOpEventPublisher>();
        });
    }
}

/// <summary>
/// Reads X-Test-UserId and X-Test-Roles headers and produces a ClaimsPrincipal.
/// Eliminates the need for a real Entra ID token in integration tests.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "TestScheme";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-UserId", out var userIdValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        var userId = userIdValues.ToString();
        var roles  = Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader)
            ? rolesHeader.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];

        var claims = new List<Claim>
        {
            new("oid",                      userId),
            new(ClaimTypes.NameIdentifier,  userId),
            new("preferred_username",       "test@example.com"),
            new("name",                     "Test User"),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>Helper extensions for authenticated requests.</summary>
public static class HttpClientExtensions
{
    public static HttpClient WithTestUser(
        this HttpClient client, Guid userId, params string[] roles)
    {
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Remove("X-Test-Roles");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        return client;
    }

    public static HttpClient WithTestUser(
        this HttpClient client, string userId, params string[] roles)
    {
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Remove("X-Test-Roles");
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        return client;
    }

    public static HttpClient WithTenant(this HttpClient client, Guid tenantId)
    {
        client.DefaultRequestHeaders.Remove("X-Tenant-Id");
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId.ToString());
        return client;
    }
}

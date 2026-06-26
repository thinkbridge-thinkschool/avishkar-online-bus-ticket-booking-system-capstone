using Azure.Monitor.OpenTelemetry.AspNetCore;
using BusBooking.Api;
using BusBooking.Api.Admin;
using BusBooking.Api.Auth;
using BusBooking.Api.Booking;
using BusBooking.Api.Buses;
using BusBooking.Api.Cities;
using BusBooking.Api.Feedback;
using BusBooking.Api.OpenApi;
using BusBooking.Api.Payments;
using BusBooking.Api.Routes;
using BusBooking.Api.Scheduling;
using BusBooking.Api.Users;
using BusBooking.Api.Vendors;
using BusBooking.Application.Common;
using BusBooking.Api.Tenants;
using BusBooking.Infrastructure;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Request size limit ────────────────────────────────────────────────────────
// Reject bodies over 64 KB at the Kestrel layer before they reach the app,
// preventing slow-loris and over-large payload attacks.
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 65_536);

// ── OpenTelemetry → Azure Monitor ─────────────────────────────────────────────
// UseAzureMonitor() reads APPLICATIONINSIGHTS_CONNECTION_STRING automatically.
// In Azure: resolved from Key Vault reference in App Service settings.
// Locally: set via user secrets — dotnet user-secrets set "APPLICATIONINSIGHTS_CONNECTION_STRING" "<value>"
// SQL dependency spans come from the bundled SqlClient instrumentation inside
// Azure.Monitor.OpenTelemetry.AspNetCore (same ADO.NET layer EF Core uses).
var otelBuilder = builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource("BusBooking.Worker")      // SeatExpiryService custom spans
        .AddSource("BusBooking.Messaging")); // ServiceBusEventPublisher custom spans
var aiConnStr = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
if (!string.IsNullOrEmpty(aiConnStr) && aiConnStr.StartsWith("InstrumentationKey="))
    otelBuilder.UseAzureMonitor();

// ── OpenAPI with Bearer security scheme ───────────────────────────────────────
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration);

var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowedOrigins = configuredOrigins
    .Prepend("http://localhost:4200")
    .Where(o => !string.IsNullOrWhiteSpace(o))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("BusBookingUi", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
              .WithHeaders("Authorization", "Content-Type", "X-Tenant-Id")
              .AllowCredentials(); // required for HttpOnly refresh-token cookie
    });
});

builder.Services.AddResponseCompression(opts =>
{
    opts.EnableForHttps = true;
    opts.Providers.Add<BrotliCompressionProvider>();
    opts.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(policy =>
        policy.With(c => !c.HttpContext.Request.Headers.ContainsKey("Authorization")));
});

// RazorpayBase: base address only — TenantRazorpayService injects per-tenant or platform credentials
// at request time, so credentials are never baked into the HTTP client at startup.
builder.Services.AddHttpClient("RazorpayBase",
    client => client.BaseAddress = new Uri("https://api.razorpay.com/v1/"));
builder.Services.AddScoped<BusBooking.Api.Payments.TenantRazorpayService>();

// ── Authentication + Authorization ───────────────────────────────────────────
// Supports two auth paths:
//   1. Entra (MSAL) — when AzureAd:ClientId is configured.
//   2. Local JWT    — when LocalJwt:SigningKey is configured.
// A PolicyScheme ("MultiScheme") inspects the token issuer before validation and
// forwards to the correct handler, so UseAuthentication() always picks the right one.
var hasAzureAdConfig  = !string.IsNullOrEmpty(builder.Configuration["AzureAd:ClientId"]);
var hasLocalJwtConfig = !string.IsNullOrEmpty(builder.Configuration["LocalJwt:SigningKey"]);

if (hasAzureAdConfig)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

    // Accept both v1 tokens (aud = clientId) and v2 tokens (aud = api://clientId)
    var clientId = builder.Configuration["AzureAd:ClientId"]!;
    builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.TokenValidationParameters.ValidAudiences =
        [
            clientId,
            $"api://{clientId}",
        ];
    });

    if (hasLocalJwtConfig)
    {
        // PolicyScheme forwards to the right handler based on the JWT issuer claim.
        builder.Services.AddAuthentication()
            .AddPolicyScheme("MultiScheme", "MultiScheme", opts =>
            {
                opts.ForwardDefaultSelector = ctx =>
                {
                    var authHeader = ctx.Request.Headers.Authorization.FirstOrDefault() ?? "";
                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var raw = authHeader["Bearer ".Length..].Trim();
                        try
                        {
                            var decoded = new JwtSecurityToken(raw);
                            if (!decoded.Issuer.Contains("microsoftonline.com",
                                    StringComparison.OrdinalIgnoreCase))
                                return "Local";
                        }
                        catch { /* malformed — let the default handler produce 401 */ }
                    }
                    return JwtBearerDefaults.AuthenticationScheme;
                };
            });

        builder.Services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultAuthenticateScheme = "MultiScheme";
            opts.DefaultChallengeScheme    = "MultiScheme";
        });
    }
}

if (hasLocalJwtConfig)
{
    var localSection = builder.Configuration.GetSection("LocalJwt");
    builder.Services.AddAuthentication()
        .AddJwtBearer("Local", opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(localSection["SigningKey"]!)),
                ValidateIssuer   = true,
                ValidIssuer      = localSection["Issuer"],
                ValidateAudience = true,
                ValidAudience    = localSection["Audience"],
                ValidateLifetime = true,
                ClockSkew        = TimeSpan.FromSeconds(30),
                NameClaimType    = "app:userId",
            };
        });

    if (!hasAzureAdConfig)
    {
        builder.Services.PostConfigure<AuthenticationOptions>(opts =>
        {
            opts.DefaultAuthenticateScheme = "Local";
            opts.DefaultChallengeScheme    = "Local";
        });
    }
}

var fallbackSchemes = new List<string>();
if (hasAzureAdConfig && hasLocalJwtConfig) fallbackSchemes.Add("MultiScheme");
else if (hasAzureAdConfig)                  fallbackSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
else if (hasLocalJwtConfig)                 fallbackSchemes.Add("Local");

builder.Services.AddAuthorization(o =>
{
    if (fallbackSchemes.Count > 0)
    {
        // Closes the "forgotten RequireAuthorization" gap for whichever schemes are active.
        o.FallbackPolicy = new AuthorizationPolicyBuilder(fallbackSchemes.ToArray())
            .RequireAuthenticatedUser()
            .Build();
    }
    // AdminOnly — cities, routes, vendor management. Merged into SuperAdmin (3-role model).
    o.AddPolicy("AdminOnly", p => p.RequireRole("BusBooking.SuperAdmin"));
    // SuperAdminOnly — platform-level tenant management (approve/reject/suspend tenants).
    o.AddPolicy("SuperAdminOnly", p => p.RequireRole("BusBooking.SuperAdmin"));
});

// ── Rate limiting ──────────────────────────────────────────────────────────────
// "api"         — 60 req/min, shared across all API endpoints (no per-IP partition)
// "auth-strict" — 5 req/min per client IP, applied to login/forgot-password/reset-password
// "auth"        — 10 req/min per client IP, applied to all other auth endpoints
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("api", l =>
    {
        l.Window      = TimeSpan.FromMinutes(1);
        l.PermitLimit = 60;
        l.QueueLimit  = 0;
    });

    // Limits are configurable so test environments can raise them without hitting false 429s
    var authStrictLimit = builder.Configuration.GetValue("RateLimits:AuthStrictPerMinute", 5);
    var authLimit       = builder.Configuration.GetValue("RateLimits:AuthPerMinute", 10);

    o.AddPolicy("auth-strict", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = authStrictLimit,
                QueueLimit  = 0,
            }));

    o.AddPolicy("auth", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window      = TimeSpan.FromMinutes(1),
                PermitLimit = authLimit,
                QueueLimit  = 0,
            }));

    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// In Development, skip real Service Bus — log events to console instead.
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IEventPublisher, NoOpEventPublisher>();

var app = builder.Build();

app.UseResponseCompression();

// ── Exception handler — must be first so it wraps all subsequent middleware ───
// Returns RFC 9110 problem-detail JSON; never exposes raw exception messages.
app.UseExceptionHandler(b => b.Run(async ctx =>
{
    ctx.Response.StatusCode      = StatusCodes.Status500InternalServerError;
    ctx.Response.ContentType     = "application/problem+json";
    await ctx.Response.WriteAsJsonAsync(new
    {
        type    = "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        title   = "An unexpected error occurred.",
        status  = 500,
        traceId = System.Diagnostics.Activity.Current?.Id ?? ctx.TraceIdentifier,
    });
}));

// ── Security headers ──────────────────────────────────────────────────────────
app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"]       = "nosniff";
    ctx.Response.Headers["X-Frame-Options"]              = "DENY";
    ctx.Response.Headers["Referrer-Policy"]              = "strict-origin-when-cross-origin";
    ctx.Response.Headers["Content-Security-Policy"]      = "default-src 'self'; script-src 'self' https://checkout.razorpay.com; style-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; font-src 'self' https://cdnjs.cloudflare.com; img-src 'self' data:; connect-src 'self' https://login.microsoftonline.com https://api.razorpay.com https://lumberjack.razorpay.com;";
    ctx.Response.Headers["Permissions-Policy"]           = "geolocation=(), microphone=(), camera=()";
    ctx.Response.Headers.StrictTransportSecurity         = "max-age=31536000";
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    // Temporary claims inspector — shows what the API sees in the JWT after processing.
    // Hit this URL while signed in to diagnose claim mapping issues.
    app.MapGet("/api/debug/claims", (HttpContext ctx) => Results.Ok(new
    {
        isAuthenticated = ctx.User.Identity?.IsAuthenticated ?? false,
        authType        = ctx.User.Identity?.AuthenticationType,
        appUserId       = ctx.User.FindFirst("app:userId")?.Value,
        claims          = ctx.User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
    })).AllowAnonymous();
}

app.UseHttpsRedirection();
app.UseCors("BusBookingUi");
app.UseRateLimiter();       // before auth so every request (including 401s) counts toward the limit
app.UseOutputCache();       // after rate limiter and before auth
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>(); // after auth so JWT claims are available
app.UseAuthorization();

app.MapTenantEndpoints();
app.MapScheduleEndpoints();
app.MapBookingEndpoints();
app.MapCityEndpoints();
app.MapRouteEndpoints();
app.MapBusEndpoints();
app.MapVendorEndpoints();
app.MapUserEndpoints();
app.MapAdminEndpoints();
app.MapPaymentEndpoints();
app.MapFeedbackEndpoints();
app.MapLocalAuthEndpoints();

// Migrations — always run; EF skips already-applied ones (idempotent, ~50ms on warm DB)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.BusBookingDbContext>();
    await db.Database.MigrateAsync();
}

// Seeding — Development always; Production only when SeedDemoData=true
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}
else if (app.Configuration.GetValue<bool>("SeedDemoData"))
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Angular SPA — serve static files from wwwroot; unmapped routes fall back to index.html
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html").WithMetadata(new AllowAnonymousAttribute());

app.Run();

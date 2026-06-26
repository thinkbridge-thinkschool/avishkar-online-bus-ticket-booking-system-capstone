using Azure.Monitor.OpenTelemetry.AspNetCore;
using BusBooking.Api;
using BusBooking.Api.Admin;
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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;

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
if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
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
              .WithHeaders("Authorization", "Content-Type", "X-Tenant-Id");
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
// When AzureAd:ClientId is configured (Azure / CI), enable full JWT Bearer validation.
// When it is absent (local dev without an Entra app registration), skip JWT auth so
// anonymous endpoints still work. Protected endpoints return 401 as expected.
var hasAzureAdConfig = !string.IsNullOrEmpty(builder.Configuration["AzureAd:ClientId"]);
if (hasAzureAdConfig)
{
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

    // Accept both v1 tokens (aud = clientId) and v2 tokens (aud = api://clientId)
    // so the app works regardless of whether requestedAccessTokenVersion is 1 or 2.
    var clientId = builder.Configuration["AzureAd:ClientId"]!;
    builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        opts.TokenValidationParameters.ValidAudiences =
        [
            clientId,
            $"api://{clientId}",
        ];
    });
}

builder.Services.AddAuthorization(o =>
{
    if (hasAzureAdConfig)
    {
        // Fallback policy: any endpoint without explicit [AllowAnonymous] requires a
        // valid Bearer token. This closes the "forgotten RequireAuthorization" gap.
        o.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    }
    // AdminOnly — cities, routes, vendor management. Merged into SuperAdmin (3-role model).
    o.AddPolicy("AdminOnly", p => p.RequireRole("BusBooking.SuperAdmin"));
    // SuperAdminOnly — platform-level tenant management (approve/reject/suspend tenants).
    o.AddPolicy("SuperAdminOnly", p => p.RequireRole("BusBooking.SuperAdmin"));
});

// ── Rate limiting: fixed window 60 req/min per policy ─────────────────────────
builder.Services.AddRateLimiter(o =>
{
    o.AddFixedWindowLimiter("api", l =>
    {
        l.Window      = TimeSpan.FromMinutes(1);
        l.PermitLimit = 60;
        l.QueueLimit  = 0;
    });
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
    ctx.Response.Headers["Content-Security-Policy"]      = "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; connect-src 'self' https://login.microsoftonline.com https://api.razorpay.com";
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
if (hasAzureAdConfig)
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

// Apply any pending EF migrations and seed in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.BusBookingDbContext>();
    await db.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

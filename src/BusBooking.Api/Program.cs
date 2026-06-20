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
using BusBooking.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
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
builder.Services.AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(tracing => tracing
        .AddSource("BusBooking.Worker")      // SeatExpiryService custom spans
        .AddSource("BusBooking.Messaging")); // ServiceBusEventPublisher custom spans

// ── OpenAPI with Bearer security scheme ───────────────────────────────────────
builder.Services.AddOpenApi(o => o.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration);

// ── Authentication + Authorization ───────────────────────────────────────────
// JWT bearer validation — reads AzureAd:TenantId / AzureAd:ClientId from config.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// Fallback policy: any endpoint without explicit [AllowAnonymous] requires a
// valid Bearer token. This closes the "forgotten RequireAuthorization" gap.
// AdminOnly policy maps to the BusBooking.Admin Entra ID app role.
builder.Services.AddAuthorization(o =>
{
    o.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    o.AddPolicy("AdminOnly", p => p.RequireClaim("roles", "BusBooking.Admin"));
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
    ctx.Response.Headers.StrictTransportSecurity         = "max-age=31536000";
    await next();
});

if (app.Environment.IsDevelopment())
    app.MapOpenApi().AllowAnonymous(); // exempt from fallback auth policy in dev

app.UseHttpsRedirection();
app.UseRateLimiter();       // before auth so every request (including 401s) counts toward the limit
app.UseAuthentication();
app.UseAuthorization();

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

// Seed on startup in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

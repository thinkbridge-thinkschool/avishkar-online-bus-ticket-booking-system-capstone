using Azure.Monitor.OpenTelemetry.AspNetCore;
using BusBooking.Api;
using BusBooking.Api.Booking;
using BusBooking.Api.Scheduling;
using BusBooking.Application.Common;
using BusBooking.Infrastructure;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

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

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddInfrastructure(builder.Configuration);

// JWT bearer validation — reads AzureAd:TenantId / AzureAd:ClientId from config.
// In dev these are empty strings, so auth is wired up but not enforced unless
// the endpoints call RequireAuthorization(). The policy only activates in prod
// where the values are injected via App Service app settings.
builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

// In Development, skip real Service Bus — log events to console instead.
if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<IEventPublisher, NoOpEventPublisher>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapScheduleEndpoints();
app.MapBookingEndpoints();

// Seed on startup in Development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<BusBooking.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.Run();

using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Assistant;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Application.Identity;
using BusBooking.Application.Tenants;
using BusBooking.Application.Buses;
using BusBooking.Application.Cities;
using BusBooking.Application.Common;
using BusBooking.Application.Feedback;
using BusBooking.Application.Payments;
using BusBooking.Application.Routes;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Application.Users;
using BusBooking.Application.Vendors;
using BusBooking.Infrastructure.Assistant;
using BusBooking.Infrastructure.BackgroundServices;
using BusBooking.Infrastructure.Caching;
using BusBooking.Infrastructure.Email;
using BusBooking.Infrastructure.Identity;
using BusBooking.Infrastructure.Messaging;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Persistence.Outbox;
using BusBooking.Infrastructure.Repositories;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace BusBooking.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // TenantContext is scoped: one instance per request, shared by the middleware
        // (which calls Resolve()) and BusBookingDbContext (which reads it for query filters).
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        services.AddDbContext<BusBookingDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection"))
                // Writes an OutboxMessage row for every raised domain event in the same
                // SaveChanges call as the business mutation — see OutboxDispatcherService
                // for the background job that actually publishes them.
                .AddInterceptors(new OutboxSavingChangesInterceptor()));

        var sbNamespace = config["ServiceBus:Namespace"];
        if (!string.IsNullOrEmpty(sbNamespace))
        {
            services.AddSingleton(_ => new ServiceBusClient(sbNamespace, new DefaultAzureCredential()));
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
            services.AddHostedService<ServiceBusConsumerService>();

            // Retries only on Service Bus's own transient failure reasons (throttling/timeout) —
            // no circuit breaker here, since this path already degrades gracefully (the Outbox
            // dispatcher's own retry-next-poll behavior is the outer safety net) and there's no
            // user-facing request left to protect once this runs out-of-band.
            services.AddResiliencePipeline("service-bus-publish", (pipeline, _) =>
            {
                var cfg = config.GetSection("Resilience:ServiceBus");
                pipeline.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<ServiceBusException>(ex =>
                        ex.Reason is ServiceBusFailureReason.ServiceBusy or ServiceBusFailureReason.ServiceTimeout),
                    MaxRetryAttempts = cfg.GetValue("RetryCount", 3),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(cfg.GetValue("BaseDelayMs", 200)),
                });
            });
        }

        services.AddScoped<ITenantRepository, TenantRepository>(); // DI binds the interface to the concrete implementation at 
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IBusRepository, BusRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();

        // Identity repositories — Phase 1
        services.AddScoped<IAppUserRepository, AppUserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Claims transformer — Phase 2; adds app:userId to every authenticated request
        services.AddMemoryCache();
        services.AddScoped<IClaimsTransformation, AppClaimsTransformer>();

        // Local auth services — Phase 3
        services.AddScoped<ILocalCredentialRepository, LocalCredentialRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Audit log — Phase 9
        services.AddScoped<IAuthAuditLogRepository, AuthAuditLogRepository>();

        // Email: SmtpEmailService when Smtp:Host is configured; LogEmailService otherwise (dev)
        if (!string.IsNullOrEmpty(config["Smtp:Host"]))
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, LogEmailService>();

        // AI Assistant — provider selected via AiAssistant:Provider ("Gemini", the default, or
        // "Groq"). Both sit behind IAiChatProvider identically from AssistantChatHandler's
        // perspective; this registration is the only place that changes to switch between them.
        // Safe to retry (a chat completion has no side effect on our data), unlike Razorpay.
        if (string.Equals(config["AiAssistant:Provider"], "Groq", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IAiChatProvider, GroqChatProvider>()
                .AddResilienceHandler("ai-chat", (pipeline, _) => ConfigureAiChatPipeline(pipeline, config));
        else
            services.AddHttpClient<IAiChatProvider, GeminiChatProvider>()
                .AddResilienceHandler("ai-chat", (pipeline, _) => ConfigureAiChatPipeline(pipeline, config));

        services.AddHostedService<SeatExpiryService>();
        services.AddHostedService<BookingCleanupService>();
        services.AddHostedService<OutboxDispatcherService>();
        services.AddScoped<DatabaseSeeder>();

        // Redis (L2) — passwordless via Entra ID, matching every other resource in this app
        // (SQL, Service Bus). AddHybridCache() always runs, degrading to L1-only in-memory
        // when Redis:HostName isn't configured (local dev), same graceful-fallback shape as
        // NoOpEventPublisher. Stampede protection (single-flight request coalescing per key)
        // is HybridCache.GetOrCreateAsync's own built-in behavior — nothing to build here.
        var redisHost = config["Redis:HostName"];
        if (!string.IsNullOrEmpty(redisHost))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.ConnectionMultiplexerFactory = async () =>
                {
                    var configurationOptions = new ConfigurationOptions { EndPoints = { { redisHost, 6380 } } };
                    await configurationOptions.ConfigureForAzureWithTokenCredentialAsync(new DefaultAzureCredential());
                    return await ConnectionMultiplexer.ConnectAsync(configurationOptions);
                };
            });
        }

        services.AddHybridCache(o => o.DefaultEntryOptions = new()
        {
            Expiration = TimeSpan.FromMinutes(5),
            LocalCacheExpiration = TimeSpan.FromMinutes(1),
        });
        services.AddScoped<ICacheService, HybridCacheService>();

        return services;
    }

    // Outer total timeout → retry (exponential backoff + jitter) → circuit breaker → inner
    // per-attempt timeout — same layering Microsoft's own AddStandardResilienceHandler() uses,
    // just with AI-chat-appropriate numbers (looser breaker than Razorpay's — these providers,
    // especially Gemini's free tier, have noisier latency).
    private static void ConfigureAiChatPipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline, IConfiguration config)
    {
        var cfg = config.GetSection("Resilience:AiChat");

        pipeline
            .AddTimeout(TimeSpan.FromSeconds(cfg.GetValue("TotalTimeoutSeconds", 20)))
            .AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = cfg.GetValue("RetryCount", 2),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(500),
            })
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = cfg.GetValue("BreakerFailureRatio", 0.6),
                MinimumThroughput = cfg.GetValue("BreakerMinThroughput", 8),
                SamplingDuration = TimeSpan.FromSeconds(60),
                BreakDuration = TimeSpan.FromSeconds(cfg.GetValue("BreakerDurationSeconds", 15)),
            })
            .AddTimeout(TimeSpan.FromSeconds(cfg.GetValue("PerAttemptTimeoutSeconds", 8)));
    }
}

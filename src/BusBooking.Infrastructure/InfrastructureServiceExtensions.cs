using Azure.Identity;
using Azure.Messaging.ServiceBus;
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
using BusBooking.Infrastructure.BackgroundServices;
using BusBooking.Infrastructure.Email;
using BusBooking.Infrastructure.Identity;
using BusBooking.Infrastructure.Messaging;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Repositories;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        var sbNamespace = config["ServiceBus:Namespace"];
        if (!string.IsNullOrEmpty(sbNamespace))
        {
            services.AddSingleton(_ => new ServiceBusClient(sbNamespace, new DefaultAzureCredential()));
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
        }

        services.AddScoped<ITenantRepository, TenantRepository>();
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

        // Email service — Phase 4; dev sends to log, prod impl added later
        services.AddScoped<IEmailService, LogEmailService>();

        services.AddHostedService<SeatExpiryService>();
        services.AddHostedService<BookingCleanupService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

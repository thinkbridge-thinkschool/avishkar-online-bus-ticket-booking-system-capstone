using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Booking.Repositories;
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
using BusBooking.Infrastructure.Messaging;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<BusBookingDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        var sbNamespace = config["ServiceBus:Namespace"];
        if (!string.IsNullOrEmpty(sbNamespace))
        {
            services.AddSingleton(_ => new ServiceBusClient(sbNamespace, new DefaultAzureCredential()));
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
        }

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<ICityRepository, CityRepository>();
        services.AddScoped<IRouteRepository, RouteRepository>();
        services.AddScoped<IBusRepository, BusRepository>();
        services.AddScoped<IVendorRepository, VendorRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IFeedbackRepository, FeedbackRepository>();

        services.AddHostedService<SeatExpiryService>();
        services.AddHostedService<BookingCleanupService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

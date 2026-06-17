using Azure.Identity;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Booking.Repositories;
using BusBooking.Application.Common;
using BusBooking.Application.Scheduling.Repositories;
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
            services.AddSingleton(_ => new ServiceBusClient(sbNamespace, new DefaultAzureCredential()));

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IScheduleRepository, ScheduleRepository>();
        services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

        services.AddHostedService<SeatExpiryService>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}

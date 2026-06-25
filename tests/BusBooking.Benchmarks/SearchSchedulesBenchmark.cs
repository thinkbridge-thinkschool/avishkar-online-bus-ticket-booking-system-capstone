using BenchmarkDotNet.Attributes;
using BusBooking.Application.Scheduling.Queries.SearchSchedules;
using BusBooking.Application.Scheduling.Repositories;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Infrastructure;
using BusBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusBooking.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class SearchSchedulesBenchmark
{
    private static readonly DateOnly BenchmarkDate = new(2026, 6, 25);

    private ServiceProvider _rootProvider = null!;
    private IServiceScope _scope = null!;
    private IScheduleRepository _repo = null!;

    [Params(50, 500)]
    public int ScheduleCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        // Register all infrastructure services (ScheduleRepository wired up internally)
        services.AddInfrastructure(config);
        services.AddLogging(b => b.AddFilter(_ => false));

        // Swap SQL Server for InMemory
        var toRemove = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<BusBookingDbContext>)
                     || (d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         && d.ServiceType.Name.Contains("IDbContextOptionsConfiguration")))
            .ToList();
        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<BusBookingDbContext>(opts =>
            opts.UseInMemoryDatabase($"bench_{ScheduleCount}_{Guid.NewGuid()}")
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        // Remove hosted background services (not needed for benchmarks)
        services.RemoveAll<IHostedService>();

        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
        _repo = _scope.ServiceProvider.GetRequiredService<IScheduleRepository>();

        var db = _scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        await SeedAsync(db, ScheduleCount);
    }

    [Benchmark(Description = "SearchSchedules")]
    public Task<IReadOnlyList<ScheduleSummaryDto>> SearchAsync() =>
        _repo.SearchAsync("Mumbai", "Pune", BenchmarkDate);

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
    }

    private static async Task SeedAsync(BusBookingDbContext db, int count)
    {
        var tenantId = Guid.NewGuid();
        var matchRoute = Route.Create("Mumbai", "Pune");
        var noiseRoute = Route.Create("Delhi", "Agra");
        db.Routes.AddRange(matchRoute, noiseRoute);

        for (var i = 0; i < count; i++)
        {
            var bus = Bus.Create($"MH-{i:D4}", $"Express {i}", BusType.Seater, 10, Guid.NewGuid(), tenantId);
            db.Buses.Add(bus);

            var schedule = Schedule.Create(
                bus.Id, matchRoute.Id, BenchmarkDate,
                new TimeOnly(8, 0), new TimeOnly(12, 0), tenantId);

            schedule.AddSeats(Enumerable.Range(1, 5)
                .Select(n => Seat.Create(schedule.Id, n, SeatType.Window, 399m)));

            db.Schedules.Add(schedule);
        }

        // 20% noise — different route, same date (won't match the search)
        for (var i = 0; i < count / 5; i++)
        {
            var bus = Bus.Create($"DL-{i:D4}", $"Delhi Express {i}", BusType.Sleeper, 10, Guid.NewGuid(), tenantId);
            db.Buses.Add(bus);

            var schedule = Schedule.Create(
                bus.Id, noiseRoute.Id, BenchmarkDate,
                new TimeOnly(9, 0), new TimeOnly(14, 0), tenantId);

            db.Schedules.Add(schedule);
        }

        await db.SaveChangesAsync();
    }
}

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
[SimpleJob(warmupCount: 5, iterationCount: 100)]
public class SearchSchedulesBenchmark
{
    private static readonly DateOnly BenchmarkDate = new(2026, 6, 25);

    // 200 schedules match the query date (BenchmarkDate, Mumbai→Pune).
    // ScheduleCount controls the number of date-noise rows layered on top.
    // This fixes the materialization cost while scaling only the scan cost,
    // making the index benefit measurable.
    private const int MatchingRows = 200;

    private ServiceProvider _rootProvider = null!;
    private IServiceScope _scope = null!;
    private IScheduleRepository _repo = null!;
    private string _dbPath = null!;

    [Params(9800)]
    public int NoiseRowCount { get; set; }

    [Params(false, true)]
    public bool WithIndex { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"busbench_{NoiseRowCount}_{WithIndex}.db");
        if (File.Exists(_dbPath)) File.Delete(_dbPath);

        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();

        services.AddInfrastructure(config);
        services.AddLogging(b => b.AddFilter(_ => false));

        // Replace SQL Server registration with SQLite so indexes are respected
        var toRemove = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<BusBookingDbContext>)
                     || (d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                         && d.ServiceType.Name.Contains("IDbContextOptionsConfiguration")))
            .ToList();
        foreach (var d in toRemove)
            services.Remove(d);

        services.AddDbContext<BusBookingDbContext>(opts =>
            opts.UseSqlite($"Data Source={_dbPath}")
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.RemoveAll<IHostedService>();

        _rootProvider = services.BuildServiceProvider();
        _scope = _rootProvider.CreateScope();
        _repo = _scope.ServiceProvider.GetRequiredService<IScheduleRepository>();

        var db = _scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (!WithIndex)
            // Drop index to simulate pre-polish state (forces full table scan)
            await db.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS \"IX_Schedules_Search\"");

        await SeedAsync(db, NoiseRowCount);
    }

    [Benchmark(Description = "SearchSchedules")]
    public Task<IReadOnlyList<ScheduleSummaryDto>> SearchAsync() =>
        _repo.SearchAsync("Mumbai", "Pune", BenchmarkDate);

    [GlobalCleanup]
    public void Cleanup()
    {
        _scope.Dispose();
        _rootProvider.Dispose();
        if (File.Exists(_dbPath))
            try { File.Delete(_dbPath); } catch { /* best-effort */ }
    }

    private static async Task SeedAsync(BusBookingDbContext db, int noiseCount)
    {
        var tenantId = Guid.NewGuid();
        var matchRoute = Route.Create("Mumbai", "Pune");
        db.Routes.Add(matchRoute);

        // One shared bus for all noise schedules — avoids inserting 9800 bus rows
        var noiseBus = Bus.Create("NOISE-000", "Noise Bus", BusType.Sleeper, 50, Guid.NewGuid(), tenantId);
        db.Buses.Add(noiseBus);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        const int batchSize = 500;
        var batch = new List<object>(batchSize * 3);

        // Matching rows — BenchmarkDate, correct route, with seats
        for (var i = 0; i < MatchingRows; i++)
        {
            var bus = Bus.Create($"MH-{i:D4}", $"Express {i}", BusType.Seater, 10, Guid.NewGuid(), tenantId);
            db.Buses.Add(bus);
            var schedule = Schedule.Create(bus.Id, matchRoute.Id, BenchmarkDate,
                new TimeOnly(8, 0), new TimeOnly(12, 0), tenantId);
            schedule.AddSeats(Enumerable.Range(1, 5)
                .Select(n => Seat.Create(schedule.Id, n, SeatType.Window, 399m)));
            db.Schedules.Add(schedule);

            if ((i + 1) % batchSize == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        // Noise rows — wrong dates, shared bus, no seats
        for (var i = 0; i < noiseCount; i++)
        {
            var date = BenchmarkDate.AddDays(i % 30 + 1);
            var schedule = Schedule.Create(noiseBus.Id, matchRoute.Id, date,
                new TimeOnly(9, 0), new TimeOnly(14, 0), tenantId);
            db.Schedules.Add(schedule);

            if ((i + 1) % batchSize == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }
        if (noiseCount % batchSize != 0)
            await db.SaveChangesAsync();
    }
}

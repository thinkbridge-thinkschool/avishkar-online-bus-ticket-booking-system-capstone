using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Domain.Tenants.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Persistence;

public sealed class DatabaseSeeder(BusBookingDbContext db, ILogger<DatabaseSeeder> logger)
{
    // Fixed demo dates — change these when you want a new demo window.
    private static readonly DateOnly Jun24 = new(2026, 6, 24);
    private static readonly DateOnly Jun25 = new(2026, 6, 25);
    private static readonly DateOnly Jun26 = new(2026, 6, 26);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // Skip only when the fixed demo dates are already present.
        try
        {
            var hasJun24 = await db.Schedules
                .IgnoreQueryFilters()
                .AnyAsync(s => s.TravelDate == Jun24, ct);

            if (hasJun24)
            {
                logger.LogInformation("Demo schedules for 2026-06-24 already present — skipping seed.");
                return;
            }
        }
        catch
        {
            // DB may not exist yet — continue to full seed below.
        }

        logger.LogInformation("Resetting database for full seed...");
        await db.Database.EnsureDeletedAsync(ct);
        await db.Database.MigrateAsync(ct);
        logger.LogInformation("Schema recreated.");

        // ── Cities ────────────────────────────────────────────────────────
        var pune   = City.Create("Pune");
        var mumbai = City.Create("Mumbai");
        var nagpur = City.Create("Nagpur");
        var nashik = City.Create("Nashik");
        await db.Cities.AddRangeAsync([pune, mumbai, nagpur, nashik], ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded 4 cities.");

        // ── Routes ────────────────────────────────────────────────────────
        var puneMumbai   = Route.Create("Pune",   "Mumbai");
        var mumbaiPune   = Route.Create("Mumbai", "Pune");
        var puneNagpur   = Route.Create("Pune",   "Nagpur");
        var nagpurPune   = Route.Create("Nagpur", "Pune");
        var mumbaiNashik = Route.Create("Mumbai", "Nashik");
        var nashikMumbai = Route.Create("Nashik", "Mumbai");
        var puneNashik   = Route.Create("Pune",   "Nashik");
        var nashikPune   = Route.Create("Nashik", "Pune");

        await db.Routes.AddRangeAsync(
            [puneMumbai, mumbaiPune, puneNagpur, nagpurPune,
             mumbaiNashik, nashikMumbai, puneNashik, nashikPune], ct);

        // ── Seed Tenant ───────────────────────────────────────────────────
        var seedTenant = Tenant.Register(
            "Demo Operator",
            "demo",
            "admin@demo.busbooking.com",
            "00000000-0000-0000-0000-000000000001");
        seedTenant.Approve();
        await db.Tenants.AddAsync(seedTenant, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded 1 tenant.");

        var seedTenantId = seedTenant.Id;

        // ── Buses (8 operators) ───────────────────────────────────────────
        var vendorId = Guid.NewGuid();
        var bus1 = Bus.Create("MH12-AB-1234", "Shivneri Express",  BusType.Seater,      40, vendorId, seedTenantId);
        var bus2 = Bus.Create("MH12-CD-5678", "Volvo Sleeper",     BusType.Sleeper,     36, vendorId, seedTenantId);
        var bus3 = Bus.Create("MH12-EF-9012", "City Link Semi",    BusType.SemiSleeper, 38, vendorId, seedTenantId);
        var bus4 = Bus.Create("MH04-GH-3456", "Orange Travels",    BusType.Seater,      45, vendorId, seedTenantId);
        var bus5 = Bus.Create("MH04-IJ-7890", "Neeta Tours VIP",   BusType.Sleeper,     40, vendorId, seedTenantId);
        var bus6 = Bus.Create("MH12-KL-2345", "Paulo Travels",     BusType.Seater,      52, vendorId, seedTenantId);
        var bus7 = Bus.Create("MH12-MN-6789", "IntrCity SmartBus", BusType.SemiSleeper, 48, vendorId, seedTenantId);
        var bus8 = Bus.Create("MH43-OP-0123", "ZingBus Express",   BusType.Sleeper,     42, vendorId, seedTenantId);

        await db.Buses.AddRangeAsync([bus1, bus2, bus3, bus4, bus5, bus6, bus7, bus8], ct);

        // ── Schedules ─────────────────────────────────────────────────────
        var schedules = new List<Domain.Scheduling.Entities.Schedule>
        {
            // ── Mumbai → Pune  24-Jun  (7 departures, full day) ───────────
            MakeSchedule(bus4.Id, mumbaiPune.Id, Jun24, T(6,  0), T(9,  30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus1.Id, mumbaiPune.Id, Jun24, T(8,  0), T(11, 30), 40, 375m, 425m, seedTenantId),
            MakeSchedule(bus3.Id, mumbaiPune.Id, Jun24, T(10, 0), T(13, 30), 38, 425m, 499m, seedTenantId),
            MakeSchedule(bus6.Id, mumbaiPune.Id, Jun24, T(14, 0), T(17, 30), 52, 349m, 399m, seedTenantId),
            MakeSchedule(bus7.Id, mumbaiPune.Id, Jun24, T(16, 0), T(19, 30), 48, 450m, 525m, seedTenantId),
            MakeSchedule(bus8.Id, mumbaiPune.Id, Jun24, T(21, 0), T(0,  30), 42, 549m, 649m, seedTenantId),
            MakeSchedule(bus5.Id, mumbaiPune.Id, Jun24, T(23, 0), T(2,  30), 40, 649m, 749m, seedTenantId),

            // ── Mumbai → Pune  25-Jun  (6 departures) ────────────────────
            MakeSchedule(bus4.Id, mumbaiPune.Id, Jun25, T(6,  0), T(9,  30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus1.Id, mumbaiPune.Id, Jun25, T(8,  0), T(11, 30), 40, 375m, 425m, seedTenantId),
            MakeSchedule(bus3.Id, mumbaiPune.Id, Jun25, T(12, 0), T(15, 30), 38, 425m, 499m, seedTenantId),
            MakeSchedule(bus6.Id, mumbaiPune.Id, Jun25, T(14, 0), T(17, 30), 52, 349m, 399m, seedTenantId),
            MakeSchedule(bus2.Id, mumbaiPune.Id, Jun25, T(22, 0), T(1,  30), 36, 549m, 649m, seedTenantId),
            MakeSchedule(bus5.Id, mumbaiPune.Id, Jun25, T(23, 0), T(2,  30), 40, 649m, 749m, seedTenantId),

            // ── Mumbai → Pune  26-Jun  (6 departures) ────────────────────
            MakeSchedule(bus4.Id, mumbaiPune.Id, Jun26, T(7,  0), T(10, 30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus1.Id, mumbaiPune.Id, Jun26, T(9,  0), T(12, 30), 40, 375m, 425m, seedTenantId),
            MakeSchedule(bus7.Id, mumbaiPune.Id, Jun26, T(11, 0), T(14, 30), 48, 450m, 525m, seedTenantId),
            MakeSchedule(bus3.Id, mumbaiPune.Id, Jun26, T(14, 0), T(17, 30), 38, 425m, 499m, seedTenantId),
            MakeSchedule(bus6.Id, mumbaiPune.Id, Jun26, T(17, 0), T(20, 30), 52, 349m, 399m, seedTenantId),
            MakeSchedule(bus2.Id, mumbaiPune.Id, Jun26, T(22, 0), T(1,  30), 36, 549m, 649m, seedTenantId),

            // ── Pune → Mumbai  24-Jun  (6 departures) ────────────────────
            MakeSchedule(bus1.Id, puneMumbai.Id, Jun24, T(6,  0),  T(9,  30), 40, 350m, 400m, seedTenantId),
            MakeSchedule(bus4.Id, puneMumbai.Id, Jun24, T(8,  0),  T(11, 30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus6.Id, puneMumbai.Id, Jun24, T(12, 0),  T(15, 30), 52, 349m, 399m, seedTenantId),
            MakeSchedule(bus7.Id, puneMumbai.Id, Jun24, T(15, 0),  T(18, 30), 48, 450m, 525m, seedTenantId),
            MakeSchedule(bus2.Id, puneMumbai.Id, Jun24, T(22, 0),  T(1,  30), 36, 549m, 649m, seedTenantId),
            MakeSchedule(bus8.Id, puneMumbai.Id, Jun24, T(23, 30), T(3,  0),  42, 549m, 649m, seedTenantId),

            // ── Pune → Mumbai  25-Jun  (5 departures) ────────────────────
            MakeSchedule(bus1.Id, puneMumbai.Id, Jun25, T(6,  0), T(9,  30), 40, 350m, 400m, seedTenantId),
            MakeSchedule(bus4.Id, puneMumbai.Id, Jun25, T(8,  0), T(11, 30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus3.Id, puneMumbai.Id, Jun25, T(14, 0), T(17, 30), 38, 425m, 499m, seedTenantId),
            MakeSchedule(bus7.Id, puneMumbai.Id, Jun25, T(17, 0), T(20, 30), 48, 450m, 525m, seedTenantId),
            MakeSchedule(bus2.Id, puneMumbai.Id, Jun25, T(22, 0), T(1,  30), 36, 549m, 649m, seedTenantId),

            // ── Pune → Mumbai  26-Jun  (4 departures) ────────────────────
            MakeSchedule(bus4.Id, puneMumbai.Id, Jun26, T(7,  0), T(10, 30), 45, 299m, 349m, seedTenantId),
            MakeSchedule(bus6.Id, puneMumbai.Id, Jun26, T(11, 0), T(14, 30), 52, 349m, 399m, seedTenantId),
            MakeSchedule(bus8.Id, puneMumbai.Id, Jun26, T(18, 0), T(21, 30), 42, 549m, 649m, seedTenantId),
            MakeSchedule(bus5.Id, puneMumbai.Id, Jun26, T(23, 0), T(2,  30), 40, 649m, 749m, seedTenantId),

            // ── Pune → Nagpur ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, puneNagpur.Id, Jun24, T(18, 0), T(6,  0), 38, 700m, 800m, seedTenantId),
            MakeSchedule(bus5.Id, puneNagpur.Id, Jun24, T(20, 0), T(8,  0), 40, 750m, 850m, seedTenantId),
            MakeSchedule(bus3.Id, puneNagpur.Id, Jun25, T(19, 0), T(7,  0), 38, 750m, 850m, seedTenantId),
            MakeSchedule(bus5.Id, puneNagpur.Id, Jun25, T(21, 0), T(9,  0), 40, 800m, 900m, seedTenantId),
            MakeSchedule(bus8.Id, puneNagpur.Id, Jun26, T(20, 0), T(8,  0), 42, 750m, 850m, seedTenantId),

            // ── Nagpur → Pune ─────────────────────────────────────────────
            MakeSchedule(bus8.Id, nagpurPune.Id, Jun24, T(8,  0), T(20, 0), 42, 700m, 800m, seedTenantId),
            MakeSchedule(bus1.Id, nagpurPune.Id, Jun25, T(7,  0), T(19, 0), 40, 700m, 800m, seedTenantId),
            MakeSchedule(bus6.Id, nagpurPune.Id, Jun26, T(9,  0), T(21, 0), 52, 699m, 799m, seedTenantId),

            // ── Mumbai → Nashik ───────────────────────────────────────────
            MakeSchedule(bus4.Id, mumbaiNashik.Id, Jun24, T(7,  0), T(10, 30), 45, 250m, 299m, seedTenantId),
            MakeSchedule(bus6.Id, mumbaiNashik.Id, Jun24, T(15, 0), T(18, 30), 52, 275m, 325m, seedTenantId),
            MakeSchedule(bus7.Id, mumbaiNashik.Id, Jun25, T(8,  0), T(11, 30), 48, 250m, 299m, seedTenantId),
            MakeSchedule(bus4.Id, mumbaiNashik.Id, Jun26, T(9,  0), T(12, 30), 45, 265m, 315m, seedTenantId),

            // ── Nashik → Mumbai ───────────────────────────────────────────
            MakeSchedule(bus7.Id, nashikMumbai.Id, Jun24, T(7,  0), T(10, 30), 48, 250m, 299m, seedTenantId),
            MakeSchedule(bus4.Id, nashikMumbai.Id, Jun25, T(8,  0), T(11, 30), 45, 275m, 325m, seedTenantId),
            MakeSchedule(bus6.Id, nashikMumbai.Id, Jun26, T(10, 0), T(13, 30), 52, 265m, 315m, seedTenantId),

            // ── Pune → Nashik ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, puneNashik.Id, Jun24, T(9,  0), T(13, 0), 38, 325m, 375m, seedTenantId),
            MakeSchedule(bus6.Id, puneNashik.Id, Jun25, T(10, 0), T(14, 0), 52, 325m, 375m, seedTenantId),
            MakeSchedule(bus1.Id, puneNashik.Id, Jun26, T(8,  0), T(12, 0), 40, 315m, 365m, seedTenantId),

            // ── Nashik → Pune ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, nashikPune.Id, Jun24, T(14, 0), T(18, 0), 38, 325m, 375m, seedTenantId),
            MakeSchedule(bus8.Id, nashikPune.Id, Jun25, T(15, 0), T(19, 0), 42, 349m, 399m, seedTenantId),
            MakeSchedule(bus5.Id, nashikPune.Id, Jun26, T(13, 0), T(17, 0), 40, 335m, 385m, seedTenantId),
        };

        await db.Schedules.AddRangeAsync(schedules, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded {Cities} cities, {Routes} routes, {Buses} buses, {Schedules} schedules for 2026-06-24 through 2026-06-26.",
            4, 8, 8, schedules.Count);
    }

    private static TimeOnly T(int h, int m) => new(h, m);

    private static Domain.Scheduling.Entities.Schedule MakeSchedule(
        Guid busId, Guid routeId,
        DateOnly date, TimeOnly departure, TimeOnly arrival,
        int totalSeats, decimal windowPrice, decimal aislePrice,
        Guid tenantId)
    {
        var schedule = Domain.Scheduling.Entities.Schedule.Create(busId, routeId, date, departure, arrival, tenantId);

        var seats = Enumerable.Range(1, totalSeats).Select(n =>
        {
            var seatType = n % 3 == 0 ? SeatType.Aisle
                         : n % 3 == 1 ? SeatType.Window
                         : SeatType.Middle;

            var price = seatType == SeatType.Window ? windowPrice
                      : seatType == SeatType.Aisle  ? aislePrice
                      : (windowPrice + aislePrice) / 2;

            return Seat.Create(schedule.Id, n, seatType, price);
        });

        schedule.AddSeats(seats);
        return schedule;
    }
}

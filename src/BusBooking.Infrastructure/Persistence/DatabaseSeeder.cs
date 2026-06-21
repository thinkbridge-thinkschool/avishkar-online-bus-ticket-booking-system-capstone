using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Persistence;

public sealed class DatabaseSeeder(BusBookingDbContext db, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        // If already fully seeded (8 buses), skip
        try
        {
            if (await db.Buses.CountAsync(ct) >= 8)
            {
                logger.LogInformation("Database already fully seeded — skipping.");
                return;
            }
        }
        catch
        {
            // DB may not exist yet — handled below
        }

        // Wipe and recreate schema so we get a clean slate
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
        var puneMumbai    = Route.Create("Pune",    "Mumbai");
        var mumbaiPune    = Route.Create("Mumbai",  "Pune");
        var puneNagpur    = Route.Create("Pune",    "Nagpur");
        var nagpurPune    = Route.Create("Nagpur",  "Pune");
        var mumbaiNashik  = Route.Create("Mumbai",  "Nashik");
        var nashikMumbai  = Route.Create("Nashik",  "Mumbai");
        var puneNashik    = Route.Create("Pune",    "Nashik");
        var nashikPune    = Route.Create("Nashik",  "Pune");

        await db.Routes.AddRangeAsync(
            [puneMumbai, mumbaiPune, puneNagpur, nagpurPune,
             mumbaiNashik, nashikMumbai, puneNashik, nashikPune], ct);

        // ── Buses (8 operators) ───────────────────────────────────────────
        var vendorId = Guid.NewGuid();
        var bus1 = Bus.Create("MH12-AB-1234", "Shivneri Express",  BusType.Seater,      40, vendorId);
        var bus2 = Bus.Create("MH12-CD-5678", "Volvo Sleeper",     BusType.Sleeper,     36, vendorId);
        var bus3 = Bus.Create("MH12-EF-9012", "City Link Semi",    BusType.SemiSleeper, 38, vendorId);
        var bus4 = Bus.Create("MH04-GH-3456", "Orange Travels",    BusType.Seater,      45, vendorId);
        var bus5 = Bus.Create("MH04-IJ-7890", "Neeta Tours VIP",   BusType.Sleeper,     40, vendorId);
        var bus6 = Bus.Create("MH12-KL-2345", "Paulo Travels",     BusType.Seater,      52, vendorId);
        var bus7 = Bus.Create("MH12-MN-6789", "IntrCity SmartBus", BusType.SemiSleeper, 48, vendorId);
        var bus8 = Bus.Create("MH43-OP-0123", "ZingBus Express",   BusType.Sleeper,     42, vendorId);

        await db.Buses.AddRangeAsync([bus1, bus2, bus3, bus4, bus5, bus6, bus7, bus8], ct);

        // ── Schedules ─────────────────────────────────────────────────────
        var today    = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);
        var dayAfter = today.AddDays(2);

        var schedules = new List<Domain.Scheduling.Entities.Schedule>
        {
            // ── Mumbai → Pune (today — 7 buses, full day coverage) ─────────
            MakeSchedule(bus4.Id, mumbaiPune.Id, today, T(6,  0), T(9,  30), 45, 299m, 349m),
            MakeSchedule(bus1.Id, mumbaiPune.Id, today, T(8,  0), T(11, 30), 40, 375m, 425m),
            MakeSchedule(bus3.Id, mumbaiPune.Id, today, T(10, 0), T(13, 30), 38, 425m, 499m),
            MakeSchedule(bus6.Id, mumbaiPune.Id, today, T(14, 0), T(17, 30), 52, 349m, 399m),
            MakeSchedule(bus7.Id, mumbaiPune.Id, today, T(16, 0), T(19, 30), 48, 450m, 525m),
            MakeSchedule(bus8.Id, mumbaiPune.Id, today, T(21, 0), T(0,  30), 42, 549m, 649m),
            MakeSchedule(bus5.Id, mumbaiPune.Id, today, T(23, 0), T(2,  30), 40, 649m, 749m),

            // ── Mumbai → Pune (tomorrow — 5 buses) ────────────────────────
            MakeSchedule(bus4.Id, mumbaiPune.Id, tomorrow, T(6,  0), T(9,  30), 45, 299m, 349m),
            MakeSchedule(bus1.Id, mumbaiPune.Id, tomorrow, T(8,  0), T(11, 30), 40, 375m, 425m),
            MakeSchedule(bus3.Id, mumbaiPune.Id, tomorrow, T(14, 0), T(17, 30), 38, 425m, 499m),
            MakeSchedule(bus2.Id, mumbaiPune.Id, tomorrow, T(22, 0), T(1,  30), 36, 549m, 649m),
            MakeSchedule(bus5.Id, mumbaiPune.Id, tomorrow, T(23, 0), T(2,  30), 40, 649m, 749m),

            // ── Mumbai → Pune (day after) ─────────────────────────────────
            MakeSchedule(bus6.Id, mumbaiPune.Id, dayAfter, T(9,  0), T(12, 30), 52, 349m, 399m),
            MakeSchedule(bus2.Id, mumbaiPune.Id, dayAfter, T(22, 0), T(1,  30), 36, 549m, 649m),

            // ── Pune → Mumbai (today — 6 buses) ───────────────────────────
            MakeSchedule(bus1.Id, puneMumbai.Id, today, T(6,  0),  T(9,  30), 40, 350m, 400m),
            MakeSchedule(bus4.Id, puneMumbai.Id, today, T(8,  0),  T(11, 30), 45, 299m, 349m),
            MakeSchedule(bus6.Id, puneMumbai.Id, today, T(12, 0),  T(15, 30), 52, 349m, 399m),
            MakeSchedule(bus7.Id, puneMumbai.Id, today, T(15, 0),  T(18, 30), 48, 450m, 525m),
            MakeSchedule(bus2.Id, puneMumbai.Id, today, T(22, 0),  T(1,  30), 36, 549m, 649m),
            MakeSchedule(bus8.Id, puneMumbai.Id, today, T(23, 30), T(3,  0),  42, 549m, 649m),

            // ── Pune → Mumbai (tomorrow) ───────────────────────────────────
            MakeSchedule(bus1.Id, puneMumbai.Id, tomorrow, T(6,  0), T(9,  30), 40, 350m, 400m),
            MakeSchedule(bus4.Id, puneMumbai.Id, tomorrow, T(8,  0), T(11, 30), 45, 299m, 349m),
            MakeSchedule(bus3.Id, puneMumbai.Id, tomorrow, T(14, 0), T(17, 30), 38, 425m, 499m),
            MakeSchedule(bus2.Id, puneMumbai.Id, tomorrow, T(22, 0), T(1,  30), 36, 549m, 649m),

            // ── Pune → Nagpur ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, puneNagpur.Id, today,    T(18, 0), T(6,  0),  38, 700m, 800m),
            MakeSchedule(bus5.Id, puneNagpur.Id, today,    T(20, 0), T(8,  0),  40, 750m, 850m),
            MakeSchedule(bus3.Id, puneNagpur.Id, tomorrow, T(19, 0), T(7,  0),  38, 750m, 850m),
            MakeSchedule(bus5.Id, puneNagpur.Id, tomorrow, T(21, 0), T(9,  0),  40, 800m, 900m),

            // ── Nagpur → Pune ─────────────────────────────────────────────
            MakeSchedule(bus8.Id, nagpurPune.Id, today,    T(8,  0), T(20, 0), 42, 700m, 800m),
            MakeSchedule(bus1.Id, nagpurPune.Id, tomorrow, T(7,  0), T(19, 0), 40, 700m, 800m),
            MakeSchedule(bus6.Id, nagpurPune.Id, dayAfter, T(9,  0), T(21, 0), 52, 699m, 799m),

            // ── Mumbai → Nashik ───────────────────────────────────────────
            MakeSchedule(bus4.Id, mumbaiNashik.Id, today,    T(7,  0), T(10, 30), 45, 250m, 299m),
            MakeSchedule(bus6.Id, mumbaiNashik.Id, today,    T(15, 0), T(18, 30), 52, 275m, 325m),
            MakeSchedule(bus7.Id, mumbaiNashik.Id, tomorrow, T(8,  0), T(11, 30), 48, 250m, 299m),

            // ── Nashik → Mumbai ───────────────────────────────────────────
            MakeSchedule(bus7.Id, nashikMumbai.Id, today,    T(7,  0), T(10, 30), 48, 250m, 299m),
            MakeSchedule(bus4.Id, nashikMumbai.Id, tomorrow, T(8,  0), T(11, 30), 45, 275m, 325m),

            // ── Pune → Nashik ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, puneNashik.Id, today,    T(9,  0), T(13, 0), 38, 325m, 375m),
            MakeSchedule(bus6.Id, puneNashik.Id, tomorrow, T(10, 0), T(14, 0), 52, 325m, 375m),

            // ── Nashik → Pune ─────────────────────────────────────────────
            MakeSchedule(bus3.Id, nashikPune.Id, today,    T(14, 0), T(18, 0), 38, 325m, 375m),
            MakeSchedule(bus8.Id, nashikPune.Id, tomorrow, T(15, 0), T(19, 0), 42, 349m, 399m),
        };

        await db.Schedules.AddRangeAsync(schedules, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Seeded {Cities} cities, {Routes} routes, {Buses} buses, {Schedules} schedules.",
            4, 8, 8, schedules.Count);
    }

    private static TimeOnly T(int h, int m) => new(h, m);

    private static Domain.Scheduling.Entities.Schedule MakeSchedule(
        Guid busId, Guid routeId,
        DateOnly date, TimeOnly departure, TimeOnly arrival,
        int totalSeats, decimal windowPrice, decimal aislePrice)
    {
        var schedule = Domain.Scheduling.Entities.Schedule.Create(busId, routeId, date, departure, arrival);

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

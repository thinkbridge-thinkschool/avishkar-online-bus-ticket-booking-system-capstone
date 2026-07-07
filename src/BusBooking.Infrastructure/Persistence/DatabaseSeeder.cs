using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Identity.Enums;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Vendor.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Persistence;

public sealed class DatabaseSeeder(BusBookingDbContext db, ILogger<DatabaseSeeder> logger)
{
    // ── Daily schedule template ───────────────────────────────────────────────
    // One canonical set of departures applied identically to every seeded date.
    // All 8 bus operators appear across the 8 routes.

    private sealed record DayTemplate(
        string BusNumber, string Source, string Destination,
        TimeOnly Departure, TimeOnly Arrival,
        int Seats, decimal WindowPrice, decimal AislePrice);

    private static readonly IReadOnlyList<DayTemplate> DailyTemplates =
    [
        // Mumbai → Pune  (6 departures, ~3.5h journey)
        new("MH04-GH-3456", "Mumbai", "Pune", T(6,  0), T(9,  30), 45, 299m, 349m),
        new("MH12-AB-1234", "Mumbai", "Pune", T(8,  0), T(11, 30), 40, 375m, 425m),
        new("MH12-EF-9012", "Mumbai", "Pune", T(10, 0), T(13, 30), 38, 425m, 499m),
        new("MH12-KL-2345", "Mumbai", "Pune", T(14, 0), T(17, 30), 52, 349m, 399m),
        new("MH43-OP-0123", "Mumbai", "Pune", T(18, 0), T(21, 30), 42, 549m, 649m),
        new("MH04-IJ-7890", "Mumbai", "Pune", T(20, 0), T(23, 30), 40, 649m, 749m),

        // Pune → Mumbai  (5 departures, ~3.5h journey)
        new("MH12-AB-1234", "Pune", "Mumbai", T(6,  0), T(9,  30), 40, 350m, 400m),
        new("MH04-GH-3456", "Pune", "Mumbai", T(8,  0), T(11, 30), 45, 299m, 349m),
        new("MH12-KL-2345", "Pune", "Mumbai", T(12, 0), T(15, 30), 52, 349m, 399m),
        new("MH12-MN-6789", "Pune", "Mumbai", T(15, 0), T(18, 30), 48, 450m, 525m),
        new("MH12-CD-5678", "Pune", "Mumbai", T(19, 0), T(22, 30), 36, 549m, 649m),

        // Pune → Nagpur  (2 departures, ~12h journey)
        new("MH12-EF-9012", "Pune", "Nagpur", T(6,  0), T(18, 0), 38, 700m, 800m),
        new("MH04-IJ-7890", "Pune", "Nagpur", T(8,  0), T(20, 0), 40, 750m, 850m),

        // Nagpur → Pune  (1 departure)
        new("MH43-OP-0123", "Nagpur", "Pune", T(8, 0), T(20, 0), 42, 700m, 800m),

        // Mumbai → Nashik  (2 departures)
        new("MH04-GH-3456", "Mumbai", "Nashik", T(7,  0), T(10, 30), 45, 250m, 299m),
        new("MH12-KL-2345", "Mumbai", "Nashik", T(15, 0), T(18, 30), 52, 275m, 325m),

        // Nashik → Mumbai  (1 departure)
        new("MH12-MN-6789", "Nashik", "Mumbai", T(7, 0), T(10, 30), 48, 250m, 299m),

        // Pune → Nashik  (1 departure)
        new("MH12-EF-9012", "Pune", "Nashik", T(9,  0), T(13, 0), 38, 325m, 375m),

        // Nashik → Pune  (1 departure)
        new("MH43-OP-0123", "Nashik", "Pune",  T(14, 0), T(18, 0), 42, 325m, 375m),
    ];

    // ── Entry point ───────────────────────────────────────────────────────────

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var today  = DateOnly.FromDateTime(DateTime.UtcNow);
        var window = Enumerable.Range(0, 14).Select(i => today.AddDays(i)).ToList();

        List<DateOnly> missingDates;
        bool hasInfrastructure;

        try
        {
            var min = window[0];
            var max = window[^1];

            var seededDates = await db.Schedules
                .IgnoreQueryFilters()
                .Where(s => s.TravelDate >= min && s.TravelDate <= max)
                .Select(s => s.TravelDate)
                .Distinct()
                .ToListAsync(ct);

            missingDates     = window.Except(seededDates).ToList();
            hasInfrastructure = await db.Tenants.IgnoreQueryFilters().AnyAsync(ct);
        }
        catch
        {
            // DB does not exist yet — treat as fully empty
            missingDates      = window;
            hasInfrastructure = false;
        }

        if (!hasInfrastructure)
            await FullSeedAsync(window, ct);
        else if (missingDates.Count > 0)
            await AddMissingSchedulesAsync(missingDates, ct);
        else
            logger.LogInformation(
                "Demo schedules are complete for {Today} through {End} — skipping schedule seed.",
                today, window[^1]);

        // Independent of schedule/tenant state above — self-heals on every startup so these
        // dev accounts exist even in a database that already had infrastructure before this
        // seeding was introduced (e.g. identity tables added by a later migration).
        await SeedDevAccountsAsync(ct);
    }

    // ── Path A: empty DB — drop, migrate, seed everything ────────────────────

    private async Task FullSeedAsync(List<DateOnly> dates, CancellationToken ct)
    {
        logger.LogInformation("Performing full seed (fresh database)...");
        await db.Database.MigrateAsync(ct);

        // Cities
        var pune   = City.Create("Pune");
        var mumbai = City.Create("Mumbai");
        var nagpur = City.Create("Nagpur");
        var nashik = City.Create("Nashik");
        await db.Cities.AddRangeAsync([pune, mumbai, nagpur, nashik], ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded 4 cities.");

        // Routes
        var routes = new[]
        {
            Route.Create("Pune",   "Mumbai"),
            Route.Create("Mumbai", "Pune"),
            Route.Create("Pune",   "Nagpur"),
            Route.Create("Nagpur", "Pune"),
            Route.Create("Mumbai", "Nashik"),
            Route.Create("Nashik", "Mumbai"),
            Route.Create("Pune",   "Nashik"),
            Route.Create("Nashik", "Pune"),
        };
        await db.Routes.AddRangeAsync(routes, ct);

        // Tenant
        var tenant = Tenant.Register(
            "Demo Operator", "demo",
            "admin@demo.busbooking.com",
            "00000000-0000-0000-0000-000000000001");
        tenant.Approve();
        await db.Tenants.AddAsync(tenant, ct);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded 1 tenant.");

        var tenantId = tenant.Id;

        // Buses
        var vendorId = Guid.NewGuid();
        var buses = new[]
        {
            Bus.Create("MH12-AB-1234", "Shivneri Express",  BusType.Seater,      40, vendorId, tenantId),
            Bus.Create("MH12-CD-5678", "Volvo Sleeper",     BusType.Sleeper,     36, vendorId, tenantId),
            Bus.Create("MH12-EF-9012", "City Link Semi",    BusType.SemiSleeper, 38, vendorId, tenantId),
            Bus.Create("MH04-GH-3456", "Orange Travels",    BusType.Seater,      45, vendorId, tenantId),
            Bus.Create("MH04-IJ-7890", "Neeta Tours VIP",   BusType.Sleeper,     40, vendorId, tenantId),
            Bus.Create("MH12-KL-2345", "Paulo Travels",     BusType.Seater,      52, vendorId, tenantId),
            Bus.Create("MH12-MN-6789", "IntrCity SmartBus", BusType.SemiSleeper, 48, vendorId, tenantId),
            Bus.Create("MH43-OP-0123", "ZingBus Express",   BusType.Sleeper,     42, vendorId, tenantId),
        };
        await db.Buses.AddRangeAsync(buses, ct);
        await db.SaveChangesAsync(ct);

        // Build maps from in-memory entities (IDs are assigned by EF after SaveChanges)
        var busMap   = buses.ToDictionary(b => b.BusNumber, b => b.Id);
        var routeMap = routes.ToDictionary(r => $"{r.Source}|{r.Destination}", r => r.Id);

        // Schedules for all 14 dates
        var schedules = dates
            .SelectMany(d => SchedulesForDate(d, busMap, routeMap, tenantId))
            .ToList();

        await db.Schedules.AddRangeAsync(schedules, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Full seed complete: 4 cities, 8 routes, 8 buses, {Count} schedules across {Days} days ({From} to {To}).",
            schedules.Count, dates.Count, dates[0], dates[^1]);
    }

    // ── Path B: infrastructure exists — insert only missing schedule dates ────

    private async Task AddMissingSchedulesAsync(List<DateOnly> missingDates, CancellationToken ct)
    {
        var busMap = await db.Buses
            .IgnoreQueryFilters()
            .ToDictionaryAsync(b => b.BusNumber, b => b.Id, ct);

        var routeMap = await db.Routes
            .IgnoreQueryFilters()
            .ToDictionaryAsync(r => $"{r.Source}|{r.Destination}", r => r.Id, ct);

        var tenantId = await db.Tenants
            .IgnoreQueryFilters()
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .FirstAsync(ct);

        var schedules = missingDates
            .SelectMany(d => SchedulesForDate(d, busMap, routeMap, tenantId))
            .ToList();

        await db.Schedules.AddRangeAsync(schedules, ct);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Partial seed: added {Count} schedules for {Days} missing date(s) ({From} to {To}).",
            schedules.Count, missingDates.Count, missingDates[0], missingDates[^1]);
    }

    // ── Dev accounts — idempotent, runs regardless of tenant/schedule state ──

    private async Task SeedDevAccountsAsync(CancellationToken ct)
    {
        // Dev SuperAdmin (local auth) — fixed ID so seed is idempotent
        // Password: Admin@123456  — NEVER use in production
        var superAdminEmail = "admin@busbooking.local";
        if (!await db.AppUsers.IgnoreQueryFilters().AnyAsync(u => u.Email == superAdminEmail, ct))
        {
            var superAdminId    = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var superAdmin      = AppUser.Create(superAdminId, superAdminEmail, "Dev SuperAdmin", emailVerified: true);
            var superAdminLogin = ExternalLogin.Create(superAdminId, LoginProvider.Local, superAdminEmail);
            var superAdminCred  = LocalCredential.Create(superAdminId,
                BCrypt.Net.BCrypt.HashPassword("Admin@123456", 12));
            var superAdminRole  = AppUserRole.Create(superAdminId, "BusBooking.SuperAdmin");
            await db.AppUsers.AddAsync(superAdmin, ct);
            await db.ExternalLogins.AddAsync(superAdminLogin, ct);
            await db.LocalCredentials.AddAsync(superAdminCred, ct);
            await db.AppUserRoles.AddAsync(superAdminRole, ct);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Dev SuperAdmin seeded — email: {Email} / password: Admin@123456 (local dev only).",
                superAdminEmail);
        }

        // Dev Vendor (local auth) — fixed ID so seed is idempotent
        // Password: BusBooking#Vendor2026!  — NEVER use in production
        // Each piece below is checked and healed independently, so a database that
        // already had an earlier piece (e.g. the identity, from a prior version of
        // this seeder) still picks up anything added later, without duplicating rows.
        var vendorUserId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var vendorEmail  = "vendor@busbooking.local";

        if (!await db.AppUsers.IgnoreQueryFilters().AnyAsync(u => u.Email == vendorEmail, ct))
        {
            var vendorUser  = AppUser.Create(vendorUserId, vendorEmail, "Dev Vendor", emailVerified: true);
            var vendorLogin = ExternalLogin.Create(vendorUserId, LoginProvider.Local, vendorEmail);
            var vendorCred  = LocalCredential.Create(vendorUserId,
                BCrypt.Net.BCrypt.HashPassword("BusBooking#Vendor2026!", 12));
            var vendorRole  = AppUserRole.Create(vendorUserId, "BusBooking.Vendor");
            await db.AppUsers.AddAsync(vendorUser, ct);
            await db.ExternalLogins.AddAsync(vendorLogin, ct);
            await db.LocalCredentials.AddAsync(vendorCred, ct);
            await db.AppUserRoles.AddAsync(vendorRole, ct);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Dev Vendor seeded — email: {Email} / password: BusBooking#Vendor2026! (local dev only).",
                vendorEmail);
        }

        // Vendor business profile — pre-approved and active so the seeded account
        // can sign in and use the vendor portal immediately, with no admin action required.
        if (!await db.Vendors.IgnoreQueryFilters().AnyAsync(v => v.Email == vendorEmail, ct))
        {
            var vendorProfile = Vendor.Register(
                entraObjectId: vendorUserId.ToString(),
                vendorName: "Shivneri Travels Pvt Ltd",
                email: vendorEmail,
                phone: "+91-9822012345",
                address: "Shop No. 14, Shivaji Nagar, Pune, Maharashtra 411005",
                licenseNumber: "MH-LIC-2026-00123");
            vendorProfile.Approve();
            vendorProfile.ClearDomainEvents(); // seeding is not a real approval workflow — no notification should fire
            await db.Vendors.AddAsync(vendorProfile, ct);
            await db.SaveChangesAsync(ct);
        }

        // Tenant — bus/schedule management requires a resolved tenant (see
        // TenantResolutionMiddleware), which resolves by matching the caller's
        // app:userId against Tenant.AdminEntraObjectId. A Vendor profile alone
        // does not grant this; every operating vendor owns exactly one tenant,
        // same as the self-service "register your tenant" flow real vendors use.
        if (!await db.Tenants.IgnoreQueryFilters().AnyAsync(t => t.AdminEntraObjectId == vendorUserId.ToString(), ct))
        {
            var vendorTenant = Tenant.Register(
                name: "Shivneri Travels Pvt Ltd",
                subdomain: "shivneri-travels",
                adminEmail: vendorEmail,
                adminEntraObjectId: vendorUserId.ToString());
            vendorTenant.Approve();
            vendorTenant.ClearDomainEvents();
            await db.Tenants.AddAsync(vendorTenant, ct);
            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Dev Vendor tenant seeded — subdomain: {Subdomain} (local dev only).",
                vendorTenant.Subdomain);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static IEnumerable<Domain.Scheduling.Entities.Schedule> SchedulesForDate(
        DateOnly date,
        Dictionary<string, Guid> busMap,
        Dictionary<string, Guid> routeMap,
        Guid tenantId)
    {
        foreach (var t in DailyTemplates)
        {
            yield return MakeSchedule(
                busMap[t.BusNumber],
                routeMap[$"{t.Source}|{t.Destination}"],
                date, t.Departure, t.Arrival,
                t.Seats, t.WindowPrice, t.AislePrice,
                tenantId);
        }
    }

    private static TimeOnly T(int h, int m) => new(h, m);

    private static Domain.Scheduling.Entities.Schedule MakeSchedule(
        Guid busId, Guid routeId,
        DateOnly date, TimeOnly departure, TimeOnly arrival,
        int totalSeats, decimal windowPrice, decimal aislePrice,
        Guid tenantId)
    {
        var schedule = Domain.Scheduling.Entities.Schedule.Create(
            busId, routeId, date, departure, arrival, tenantId);

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

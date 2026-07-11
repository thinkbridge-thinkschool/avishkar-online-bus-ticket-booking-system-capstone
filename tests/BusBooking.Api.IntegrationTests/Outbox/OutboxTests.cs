using BusBooking.Application.Common;
using BusBooking.Domain.Booking.Events;
using BusBooking.Domain.Booking.ValueObjects;
using BusBooking.Domain.Common;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Infrastructure.BackgroundServices;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Persistence.Outbox;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Api.IntegrationTests.Outbox;

public sealed class OutboxTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public OutboxTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private BusBookingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BusBookingDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new OutboxSavingChangesInterceptor())
            .Options;
        var db = new BusBookingDbContext(options, new TenantContext());
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task SavingChanges_WithPendingDomainEvent_WritesOutboxRowAndClearsEvent()
    {
        using var db = CreateContext();
        var tenantId = Guid.NewGuid();
        var booking = BookingAggregate.Create(
            Guid.NewGuid(), "user@example.com", Guid.NewGuid(),
            [new BookedSeat(1, "Passenger", 30, "Female", 500m, null, null)], tenantId);
        booking.AwaitPayment();
        booking.ClearDomainEvents();
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        // Confirm() raises BookingConfirmedEvent — the interceptor must turn it into an
        // OutboxMessage row as part of THIS SaveChangesAsync, then clear it from the entity,
        // with no handler ever calling PublishAsync/ClearDomainEvents itself.
        booking.Confirm("Test User");
        await db.SaveChangesAsync();

        var rows = await db.OutboxMessages.ToListAsync();
        var row = Assert.Single(rows);
        Assert.Equal(nameof(BookingConfirmedEvent), row.EventType);
        Assert.Null(row.ProcessedAt);
        Assert.Empty(booking.DomainEvents);
    }

    [Fact]
    public async Task BookingCleanupService_CancelsExpiredBooking_WritesOutboxRowForCancelledEvent()
    {
        // Regression test for the fix in BookingCleanupService: it used to call
        // booking.ClearDomainEvents() itself after Cancel(), silently dropping
        // BookingCancelledEvent since nothing ever published it. Now the interceptor
        // (registered on the DbContext below) is what clears events, only after
        // recording them as Outbox rows.
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddScoped<BusBooking.Application.Common.ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddDbContext<BusBookingDbContext>(opts => opts
            .UseSqlite(_connection)
            .AddInterceptors(new OutboxSavingChangesInterceptor()));
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
            db.Database.EnsureCreated();

            var tenantId = Guid.NewGuid();
            var route = Route.Create($"From-{Guid.NewGuid():N}", $"To-{Guid.NewGuid():N}");
            var bus = Bus.Create($"MH-OB-{Guid.NewGuid():N}"[..18], "Outbox Test Bus", BusType.Seater, 2, Guid.NewGuid(), tenantId);
            var schedule = Schedule.Create(bus.Id, route.Id, new DateOnly(2026, 8, 1), new TimeOnly(8, 0), new TimeOnly(12, 0), tenantId);
            var seat = Seat.Create(schedule.Id, 1, SeatType.Window, 500m);
            seat.Reserve();
            schedule.AddSeats([seat]);
            db.Routes.Add(route);
            db.Buses.Add(bus);
            db.Schedules.Add(schedule);

            var booking = BookingAggregate.Create(
                Guid.NewGuid(), "user@example.com", schedule.Id,
                [new BookedSeat(1, "Passenger", 30, "Female", 500m, null, null)], tenantId);
            booking.AwaitPayment();
            booking.ClearDomainEvents();
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            // Backdate BookedAt past the 30-minute payment timeout — no public API sets this,
            // it's always DateTime.UtcNow at creation, so bypass the private setter for the test.
            typeof(BookingAggregate).GetProperty(nameof(BookingAggregate.BookedAt))!
                .GetSetMethod(nonPublic: true)!.Invoke(booking, [DateTime.UtcNow.AddHours(-1)]);
            await db.SaveChangesAsync();
        }

        var cleanupService = new BookingCleanupService(
            new TestScopeFactory(provider), NullLogger<BookingCleanupService>.Instance);
        await cleanupService.CancelExpiredPaymentPendingBookingsAsync(CancellationToken.None);

        using var verifyScope = provider.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var rows = await verifyDb.OutboxMessages.ToListAsync();
        Assert.Contains(rows, r => r.EventType == nameof(BookingCancelledEvent));
    }

    [Fact]
    public async Task OutboxDispatcherService_RetriesFailedMessage_ThenSucceeds()
    {
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddScoped<BusBooking.Application.Common.ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
        services.AddDbContext<BusBookingDbContext>(opts => opts
            .UseSqlite(_connection)
            .AddInterceptors(new OutboxSavingChangesInterceptor()));
        var flakyPublisher = new FlakyEventPublisher(failFirstNCalls: 1);
        services.AddScoped<IEventPublisher>(_ => flakyPublisher);
        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
            db.Database.EnsureCreated();

            var tenantId = Guid.NewGuid();
            var booking = BookingAggregate.Create(
                Guid.NewGuid(), "user@example.com", Guid.NewGuid(),
                [new BookedSeat(1, "Passenger", 30, "Female", 500m, null, null)], tenantId);
            booking.AwaitPayment();
            booking.ClearDomainEvents();
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            booking.Confirm("Test User");
            await db.SaveChangesAsync();
        }

        var dispatcher = new OutboxDispatcherService(
            new TestScopeFactory(provider), NullLogger<OutboxDispatcherService>.Instance);

        // Attempt 1 — publisher throws, message stays pending with Attempts incremented.
        await dispatcher.DispatchPendingAsync(CancellationToken.None);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
            var message = await db.OutboxMessages.SingleAsync();
            Assert.Equal(1, message.Attempts);
            Assert.Null(message.ProcessedAt);
            Assert.False(message.DeadLettered);
        }

        // Attempt 2 — publisher succeeds, message is marked processed.
        await dispatcher.DispatchPendingAsync(CancellationToken.None);
        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
            var message = await db.OutboxMessages.SingleAsync();
            Assert.NotNull(message.ProcessedAt);
        }
    }

    private sealed class FlakyEventPublisher(int failFirstNCalls) : IEventPublisher
    {
        private int _calls;

        public Task PublishAsync<T>(T domainEvent, Guid? messageId = null, CancellationToken ct = default) where T : IDomainEvent
        {
            _calls++;
            if (_calls <= failFirstNCalls)
                throw new InvalidOperationException("Simulated transient publish failure.");
            return Task.CompletedTask;
        }
    }

    private sealed class TestScopeFactory(IServiceProvider provider) : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => provider.CreateScope();
    }
}

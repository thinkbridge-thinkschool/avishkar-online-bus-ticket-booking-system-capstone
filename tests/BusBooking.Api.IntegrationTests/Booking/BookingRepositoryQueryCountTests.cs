using System.Data.Common;
using BusBooking.Domain.Booking.ValueObjects;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Repositories;
using BusBooking.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using BookingAggregate = BusBooking.Domain.Booking.Aggregates.Booking;

namespace BusBooking.Api.IntegrationTests.Booking;

// Proves the N+1 fix: GetByUserIdWithDetailsAsync must issue exactly one SQL query
// regardless of booking count, unlike the old "load bookings, then 3 more queries per
// booking via BookingDtoFactory" loop. Needs a real relational provider (SQLite) since
// the InMemory provider used elsewhere in this project never creates a DbCommand.
public sealed class BookingRepositoryQueryCountTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly BusBookingDbContext _db;
    private readonly CountingCommandInterceptor _interceptor = new();

    public BookingRepositoryQueryCountTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<BusBookingDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_interceptor)
            .Options;

        _db = new BusBookingDbContext(options, new TenantContext());
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetByUserIdWithDetailsAsync_IssuesExactlyOneQuery_RegardlessOfBookingCount()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
        {
            var route = Route.Create($"From-{i}-{Guid.NewGuid():N}", $"To-{i}-{Guid.NewGuid():N}");
            var bus = Bus.Create($"MH-QC-{Guid.NewGuid():N}"[..18], $"Bus {i}", BusType.Seater, 2, Guid.NewGuid(), tenantId);
            var schedule = Schedule.Create(
                bus.Id, route.Id, new DateOnly(2026, 8, 1), new TimeOnly(8, 0), new TimeOnly(12, 0), tenantId);
            schedule.AddSeats([Seat.Create(schedule.Id, 1, SeatType.Window, 500m)]);

            _db.Routes.Add(route);
            _db.Buses.Add(bus);
            _db.Schedules.Add(schedule);

            var booking = BookingAggregate.Create(
                userId, "user@example.com", schedule.Id,
                [new BookedSeat(1, "Passenger", 30, "Female", 500m, null, null)],
                tenantId);
            booking.AwaitPayment();
            booking.ClearDomainEvents();
            _db.Bookings.Add(booking);
        }
        await _db.SaveChangesAsync();

        _interceptor.Reset();
        var repo = new BookingRepository(_db);
        var dtos = await repo.GetByUserIdWithDetailsAsync(userId);

        Assert.Equal(5, dtos.Count);
        Assert.All(dtos, dto => Assert.NotNull(dto.FromCityName));
        Assert.Equal(1, _interceptor.ReaderExecutingCount);
    }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        public int ReaderExecutingCount { get; private set; }

        public void Reset() => ReaderExecutingCount = 0;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            ReaderExecutingCount++;
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            ReaderExecutingCount++;
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

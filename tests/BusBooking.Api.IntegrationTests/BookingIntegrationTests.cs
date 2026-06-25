using System.Net;
using System.Text;
using System.Text.Json;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests;

public sealed class BookingIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public BookingIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private Guid SeedScheduleWithOneSeat()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();

        var tenantId = Guid.NewGuid();
        var route    = Route.Create($"From-{Guid.NewGuid():N}", $"To-{Guid.NewGuid():N}");
        db.Routes.Add(route);
        var bus = Bus.Create($"MH-BK-{Guid.NewGuid():N}"[..18], "Booking Test Bus", BusType.Seater, 2, Guid.NewGuid(), tenantId);
        db.Buses.Add(bus);
        var schedule = Domain.Scheduling.Entities.Schedule.Create(
            bus.Id, route.Id,
            new DateOnly(2026, 6, 26),
            new TimeOnly(8, 0),
            new TimeOnly(12, 0),
            tenantId);
        schedule.AddSeats([
            Seat.Create(schedule.Id, 1, SeatType.Window, 500m),
        ]);
        db.Schedules.Add(schedule);
        db.SaveChanges();
        return schedule.Id;
    }

    [Fact]
    public async Task Post_Booking_WithValidSeat_ShouldReturn201Created()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var client = _factory.CreateClient().WithTestUser(Guid.NewGuid());

        var body = JsonSerializer.Serialize(new
        {
            scheduleId,
            seats = new[]
            {
                new { seatNumber = 1, passengerName = "Alice", passengerAge = 30, passengerGender = "Female" },
            },
        });

        var response = await client.PostAsync(
            "/api/v1/bookings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Post_Booking_Unauthenticated_ShouldReturn401()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var body = JsonSerializer.Serialize(new
        {
            scheduleId,
            seats = new[]
            {
                new { seatNumber = 1, passengerName = "Bob", passengerAge = 25, passengerGender = "Male" },
            },
        });

        var response = await _factory.CreateClient().PostAsync(
            "/api/v1/bookings",
            new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Post_Booking_DoubleBooking_ShouldReturn409Conflict()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var userId = Guid.NewGuid();
        var client = _factory.CreateClient().WithTestUser(userId);

        var body = JsonSerializer.Serialize(new
        {
            scheduleId,
            seats = new[]
            {
                new { seatNumber = 1, passengerName = "Carol", passengerAge = 28, passengerGender = "Female" },
            },
        });

        // First booking should succeed
        var first = await client.PostAsync(
            "/api/v1/bookings",
            new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second booking for the same reserved seat should conflict
        var second = await client.PostAsync(
            "/api/v1/bookings",
            new StringContent(body, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}

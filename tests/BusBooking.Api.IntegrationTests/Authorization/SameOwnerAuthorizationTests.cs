using System.Net;
using System.Text;
using System.Text.Json;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests.Authorization;

// Regression coverage for the IDOR fix: "get my X by userId" endpoints must reject a
// caller who isn't the owner and isn't a SuperAdmin, via the SameOwner authorization policy.
public sealed class SameOwnerAuthorizationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public SameOwnerAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private Guid SeedScheduleWithOneSeat()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();

        var tenantId = Guid.NewGuid();
        var route = Route.Create($"From-{Guid.NewGuid():N}", $"To-{Guid.NewGuid():N}");
        db.Routes.Add(route);
        var bus = Bus.Create($"MH-SO-{Guid.NewGuid():N}"[..18], "SameOwner Test Bus", BusType.Seater, 2, Guid.NewGuid(), tenantId);
        db.Buses.Add(bus);
        var schedule = Schedule.Create(
            bus.Id, route.Id,
            new DateOnly(2026, 7, 20),
            new TimeOnly(9, 0),
            new TimeOnly(13, 0),
            tenantId);
        schedule.AddSeats([Seat.Create(schedule.Id, 1, SeatType.Window, 500m)]);
        db.Schedules.Add(schedule);
        db.SaveChanges();
        return schedule.Id;
    }

    private async Task<Guid> CreateBookingAsAsync(Guid ownerId, Guid scheduleId)
    {
        var client = _factory.CreateClient().WithTestUser(ownerId);
        var body = JsonSerializer.Serialize(new
        {
            scheduleId,
            seats = new[] { new { seatNumber = 1, passengerName = "Owner", passengerAge = 30, passengerGender = "Female" } },
        });

        var response = await client.PostAsync(
            "/api/v1/bookings", new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("bookingId").GetGuid();
    }

    [Theory]
    [InlineData("/api/v1/bookings/user/{0}")]
    [InlineData("/api/v1/payments/user/{0}")]
    [InlineData("/api/v1/feedback/user/{0}")]
    public async Task GetByUserId_SameUser_ReturnsOk(string routeTemplate)
    {
        var ownerId = Guid.NewGuid();
        var client = _factory.CreateClient().WithTestUser(ownerId);

        var response = await client.GetAsync(string.Format(routeTemplate, ownerId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/bookings/user/{0}")]
    [InlineData("/api/v1/payments/user/{0}")]
    [InlineData("/api/v1/feedback/user/{0}")]
    public async Task GetByUserId_DifferentNonAdminUser_ReturnsForbidden(string routeTemplate)
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var client = _factory.CreateClient().WithTestUser(callerId);

        var response = await client.GetAsync(string.Format(routeTemplate, ownerId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/bookings/user/{0}")]
    [InlineData("/api/v1/payments/user/{0}")]
    [InlineData("/api/v1/feedback/user/{0}")]
    public async Task GetByUserId_DifferentSuperAdminUser_ReturnsOk(string routeTemplate)
    {
        var ownerId = Guid.NewGuid();
        var callerId = Guid.NewGuid();
        var client = _factory.CreateClient().WithTestUser(callerId, "BusBooking.SuperAdmin");

        var response = await client.GetAsync(string.Format(routeTemplate, ownerId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingById_Owner_ReturnsOk()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var ownerId = Guid.NewGuid();
        var bookingId = await CreateBookingAsAsync(ownerId, scheduleId);

        var response = await _factory.CreateClient().WithTestUser(ownerId)
            .GetAsync($"/api/v1/bookings/{bookingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingById_DifferentNonAdminUser_ReturnsForbidden()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var ownerId = Guid.NewGuid();
        var bookingId = await CreateBookingAsAsync(ownerId, scheduleId);

        var response = await _factory.CreateClient().WithTestUser(Guid.NewGuid())
            .GetAsync($"/api/v1/bookings/{bookingId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingById_DifferentSuperAdminUser_ReturnsOk()
    {
        var scheduleId = SeedScheduleWithOneSeat();
        var ownerId = Guid.NewGuid();
        var bookingId = await CreateBookingAsAsync(ownerId, scheduleId);

        var response = await _factory.CreateClient().WithTestUser(Guid.NewGuid(), "BusBooking.SuperAdmin")
            .GetAsync($"/api/v1/bookings/{bookingId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

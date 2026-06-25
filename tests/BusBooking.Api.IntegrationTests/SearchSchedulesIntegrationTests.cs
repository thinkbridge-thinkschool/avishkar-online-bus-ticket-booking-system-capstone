using System.Net;
using System.Text.Json;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests;

/// <summary>
/// Tests for GET /api/v1/schedules/search across various query scenarios.
/// </summary>
public sealed class SearchSchedulesIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly ApiFactory _factory;

    public SearchSchedulesIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private void SeedCitiesAndSchedule()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();

        if (db.Cities.Any(c => c.CityName == "Mumbai")) return;

        var mumbai = City.Create("Mumbai");
        var pune   = City.Create("Pune");
        db.Cities.AddRange(mumbai, pune);

        var tenantId = Guid.NewGuid();
        var route    = Route.Create("Mumbai", "Pune");
        db.Routes.Add(route);

        var bus = Bus.Create("MH-01-TEST-1111", "Test Bus", BusType.Seater, 10, Guid.NewGuid(), tenantId);
        db.Buses.Add(bus);

        var schedule = Domain.Scheduling.Entities.Schedule.Create(
            bus.Id, route.Id,
            new DateOnly(2026, 6, 24),
            new TimeOnly(8, 0),
            new TimeOnly(11, 0),
            tenantId);
        schedule.AddSeats([
            Seat.Create(schedule.Id, 1, SeatType.Window, 399m),
            Seat.Create(schedule.Id, 2, SeatType.Aisle,  449m),
        ]);
        db.Schedules.Add(schedule);
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_WithValidCityIds_ShouldReturn200AndNonEmptyArray()
    {
        SeedCitiesAndSchedule();
        using var scope = _factory.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var mumbai = db.Cities.First(c => c.CityName == "Mumbai");
        var pune   = db.Cities.First(c => c.CityName == "Pune");

        var response = await _client.GetAsync(
            $"/api/v1/schedules/search?fromCityId={mumbai.Id}&toCityId={pune.Id}&travelDate=2026-06-24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var arr  = JsonSerializer.Deserialize<JsonElement[]>(body);
        Assert.NotNull(arr);
        Assert.NotEmpty(arr);
    }

    [Fact]
    public async Task Get_WithUnknownFromCityId_ShouldReturn404()
    {
        SeedCitiesAndSchedule();
        using var scope = _factory.Services.CreateScope();
        var db   = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var pune = db.Cities.First(c => c.CityName == "Pune");

        var response = await _client.GetAsync(
            $"/api/v1/schedules/search?fromCityId={Guid.NewGuid()}&toCityId={pune.Id}&travelDate=2026-06-24");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_WithInvalidDateFormat_ShouldReturn400()
    {
        SeedCitiesAndSchedule();
        using var scope = _factory.Services.CreateScope();
        var db     = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var mumbai = db.Cities.First(c => c.CityName == "Mumbai");
        var pune   = db.Cities.First(c => c.CityName == "Pune");

        var response = await _client.GetAsync(
            $"/api/v1/schedules/search?fromCityId={mumbai.Id}&toCityId={pune.Id}&travelDate=not-a-date");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

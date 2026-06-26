using System.Net;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests;

public sealed class RateLimitingIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public RateLimitingIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private void SeedCitiesForRateTest()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();
        if (!db.Cities.Any(c => c.CityName == "RateFrom"))
        {
            db.Cities.AddRange(City.Create("RateFrom"), City.Create("RateTo"));
            db.SaveChanges();
        }
    }

    [Fact]
    public async Task SearchSchedules_After60Requests_ShouldReturn429()
    {
        SeedCitiesForRateTest();
        using var scope = _factory.Services.CreateScope();
        var db    = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var from  = db.Cities.First(c => c.CityName == "RateFrom");
        var to    = db.Cities.First(c => c.CityName == "RateTo");
        var url   = $"/api/v1/schedules/search?fromCityId={from.Id}&toCityId={to.Id}&travelDate=2026-06-24";

        // Each test client gets a new HttpClient so rate limiting is per-test.
        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        HttpStatusCode last = HttpStatusCode.OK;
        for (int i = 0; i < 61; i++)
            last = (await client.GetAsync(url)).StatusCode;

        Assert.Equal(HttpStatusCode.TooManyRequests, last);
    }
}

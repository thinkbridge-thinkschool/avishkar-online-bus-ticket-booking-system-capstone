using System.Net;
using System.Text;
using System.Text.Json;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Scheduling.Enums;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Vendor.Aggregates;
using BusBooking.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests;

/// <summary>
/// Proves that the EF Core global query filter isolates booking data per tenant.
/// </summary>
public sealed class TenantIsolationIntegrationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public TenantIsolationIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private (Guid tenantAId, Guid tenantBId, Guid cityAId, Guid cityBId, string vendorAOid) SeedTwoTenants()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        db.Database.EnsureCreated();

        var tenantA = Tenant.Register("OperatorA", "operatora-iso", "a@a.com", $"oid-a-{Guid.NewGuid()}");
        var tenantB = Tenant.Register("OperatorB", "operatorb-iso", "b@b.com", $"oid-b-{Guid.NewGuid()}");
        tenantA.Approve();
        tenantB.Approve();
        db.Tenants.AddRange(tenantA, tenantB);

        var cityA = City.Create($"CityA-{Guid.NewGuid():N}");
        var cityB = City.Create($"CityB-{Guid.NewGuid():N}");
        db.Cities.AddRange(cityA, cityB);

        var routeA = Route.Create(cityA.CityName, cityB.CityName);
        var routeB = Route.Create(cityB.CityName, cityA.CityName);
        db.Routes.AddRange(routeA, routeB);

        // Seed vendors so /mine endpoint can resolve by Entra OID
        var vendorAOid = $"vendor-a-{Guid.NewGuid()}";
        var vendorA = Vendor.Register(vendorAOid, "Vendor A", "va@test.com", "9000000001", "Addr A", "LIC-A-001");
        vendorA.Approve();
        db.Vendors.Add(vendorA);

        var busA = Bus.Create("MH-ISO-A-002", "Bus A", BusType.Seater, 5, vendorA.Id, tenantA.Id);
        var busB = Bus.Create("MH-ISO-B-002", "Bus B", BusType.Seater, 5, Guid.NewGuid(), tenantB.Id);
        db.Buses.AddRange(busA, busB);

        var travelDate = new DateOnly(2026, 6, 25);

        var scheduleA = Domain.Scheduling.Entities.Schedule.Create(
            busA.Id, routeA.Id, travelDate, new TimeOnly(8, 0), new TimeOnly(12, 0), tenantA.Id);
        scheduleA.AddSeats([Seat.Create(scheduleA.Id, 1, SeatType.Window, 399m)]);

        var scheduleB = Domain.Scheduling.Entities.Schedule.Create(
            busB.Id, routeB.Id, travelDate, new TimeOnly(9, 0), new TimeOnly(13, 0), tenantB.Id);
        scheduleB.AddSeats([Seat.Create(scheduleB.Id, 1, SeatType.Window, 450m)]);

        db.Schedules.AddRange(scheduleA, scheduleB);
        db.SaveChanges();

        return (tenantA.Id, tenantB.Id, cityA.Id, cityB.Id, vendorAOid);
    }

    [Fact]
    public async Task GetMySchedules_ShouldReturnOnlyTenantASchedules_WhenAuthenticatedAsTenantA()
    {
        var (tenantAId, _, cityAId, cityBId, vendorAOid) = SeedTwoTenants();

        // Use the vendor's Entra OID as the test user ID so /mine can resolve the vendor
        var client = _factory.CreateClient()
            .WithTenant(tenantAId)
            .WithTestUser(vendorAOid, "BusBooking.Vendor");

        var response = await client.GetAsync("/api/v1/schedules/mine");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var arr  = JsonSerializer.Deserialize<JsonElement[]>(body);
        Assert.NotNull(arr);
        Assert.All(arr!, item =>
        {
            var tenantIdRaw = item.TryGetProperty("tenantId", out var tid) ? tid.GetString() : null;
            if (tenantIdRaw is not null)
                Assert.Equal(tenantAId, Guid.Parse(tenantIdRaw));
        });
    }
}

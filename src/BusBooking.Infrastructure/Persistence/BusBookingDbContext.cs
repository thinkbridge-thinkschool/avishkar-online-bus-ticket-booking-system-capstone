using BusBooking.Application.Common;
using BusBooking.Domain.Booking.Aggregates;
using BusBooking.Domain.Booking.Entities;
using BusBooking.Domain.Feedback.Entities;
using BusBooking.Domain.Identity.Entities;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Tenants.Aggregates;
using BusBooking.Domain.Users.Entities;
using BusBooking.Domain.Vendor.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Persistence;

public sealed class BusBookingDbContext(
    DbContextOptions<BusBookingDbContext> options,
    ITenantContext tenantContext) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Domain.Scheduling.Entities.Schedule> Schedules => Set<Domain.Scheduling.Entities.Schedule>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Bus> Buses => Set<Bus>();
    public DbSet<Route> Routes => Set<Route>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<FeedbackEntry> FeedbackEntries => Set<FeedbackEntry>();

    // Unified identity tables — Phase 1 dual-auth foundation
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<LocalCredential> LocalCredentials => Set<LocalCredential>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AppUserRole> AppUserRoles => Set<AppUserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BusBookingDbContext).Assembly);

        // Row-level tenant isolation. When IsResolved is false (Super Admin / platform request),
        // the first clause short-circuits to true and the filter is effectively disabled.
        modelBuilder.Entity<Bus>()
            .HasQueryFilter(b => !tenantContext.IsResolved || b.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Domain.Scheduling.Entities.Schedule>()
            .HasQueryFilter(s => !tenantContext.IsResolved || s.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Booking>()
            .HasQueryFilter(b => !tenantContext.IsResolved || b.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Payment>()
            .HasQueryFilter(p => !tenantContext.IsResolved || p.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<FeedbackEntry>()
            .HasQueryFilter(f => !tenantContext.IsResolved || f.TenantId == tenantContext.TenantId);
    }
}

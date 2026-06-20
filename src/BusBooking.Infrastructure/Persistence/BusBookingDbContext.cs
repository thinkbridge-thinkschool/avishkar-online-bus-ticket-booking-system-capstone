using BusBooking.Domain.Booking.Aggregates;
using BusBooking.Domain.Booking.Entities;
using BusBooking.Domain.Feedback.Entities;
using BusBooking.Domain.Scheduling.Entities;
using BusBooking.Domain.Users.Entities;
using BusBooking.Domain.Vendor.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Infrastructure.Persistence;

public sealed class BusBookingDbContext(DbContextOptions<BusBookingDbContext> options) : DbContext(options)
{
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BusBookingDbContext).Assembly);
    }
}

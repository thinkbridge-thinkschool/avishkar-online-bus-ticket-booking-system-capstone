using BusBooking.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.IsActive).HasDefaultValue(true);

        builder.HasMany(s => s.Seats)
               .WithOne()
               .HasForeignKey(nameof(Seat.ScheduleId))
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.TravelDate, s.IsActive, s.TenantId })
               .IncludeProperties(s => new { s.RouteId, s.BusId })
               .HasDatabaseName("IX_Schedules_Search");
        builder.Ignore(s => s.AvailableSeatsCount);
        builder.Navigation(s => s.Seats).AutoInclude();
    }
}

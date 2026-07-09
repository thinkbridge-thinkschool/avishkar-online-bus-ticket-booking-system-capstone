using BusBooking.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(c => c.Id); // Id is Primary Key
        builder.Property(c => c.CityName).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.CityName).IsUnique(); // Not allowed pune-> pune
        builder.Ignore(c => c.DomainEvents); // Don't save domain events to the database. Domain events are used for communication between different parts of the application and are not meant to be persisted in the database.
    }
}

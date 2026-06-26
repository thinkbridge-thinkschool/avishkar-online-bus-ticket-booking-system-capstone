using BusBooking.Domain.Scheduling.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CityName).HasMaxLength(100).IsRequired();
        builder.HasIndex(c => c.CityName).IsUnique();
        builder.Ignore(c => c.DomainEvents);
    }
}

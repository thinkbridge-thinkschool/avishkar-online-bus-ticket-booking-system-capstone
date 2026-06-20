using BusBooking.Domain.Vendor.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.EntraObjectId).HasMaxLength(36).IsRequired();
        builder.Property(v => v.VendorName).HasMaxLength(200).IsRequired();
        builder.Property(v => v.Email).HasMaxLength(256).IsRequired();
        builder.Property(v => v.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(v => v.Address).HasMaxLength(500).IsRequired();
        builder.Property(v => v.LicenseNumber).HasMaxLength(50).IsRequired();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(v => v.EntraObjectId).IsUnique();
        builder.HasIndex(v => v.Email).IsUnique();
        builder.Ignore(v => v.DomainEvents);
    }
}

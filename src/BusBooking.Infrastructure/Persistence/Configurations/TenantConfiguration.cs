using BusBooking.Domain.Tenants.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Subdomain).HasMaxLength(30).IsRequired();
        builder.Property(t => t.AdminEntraObjectId).HasMaxLength(36).IsRequired();
        builder.Property(t => t.AdminEmail).HasMaxLength(256).IsRequired();
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.RazorpayKeyId).HasMaxLength(50);
        builder.Property(t => t.RazorpayKeySecret).HasMaxLength(100);
        builder.HasIndex(t => t.Subdomain).IsUnique();
        builder.HasIndex(t => t.AdminEntraObjectId).IsUnique();
        builder.Ignore(t => t.DomainEvents);
        // TenantId is a computed alias for Id — not a separate column
        builder.Ignore(t => t.TenantId);
    }
}

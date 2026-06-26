using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.LoginProvider)
            .IsRequired();

        builder.Property(l => l.ProviderKey)
            .HasMaxLength(256)
            .IsRequired();

        // Unique per provider — prevents the same external account being linked twice
        builder.HasIndex(l => new { l.LoginProvider, l.ProviderKey })
            .IsUnique()
            .HasDatabaseName("IX_ExternalLogins_Provider_Key");

        builder.HasIndex(l => l.AppUserId)
            .HasDatabaseName("IX_ExternalLogins_AppUserId");
    }
}

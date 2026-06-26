using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUsers");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.DisplayName)
            .HasMaxLength(256)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_AppUsers_Email");

        builder.HasMany(u => u.ExternalLogins)
            .WithOne(l => l.AppUser)
            .HasForeignKey(l => l.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.LocalCredential)
            .WithOne(l => l.AppUser)
            .HasForeignKey<LocalCredential>(l => l.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Roles)
            .WithOne(r => r.AppUser)
            .HasForeignKey(r => r.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(t => t.AppUser)
            .HasForeignKey(t => t.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

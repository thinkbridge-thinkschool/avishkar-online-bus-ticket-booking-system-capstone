using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    public void Configure(EntityTypeBuilder<AppUserRole> builder)
    {
        builder.ToTable("AppUserRoles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoleName)
            .HasMaxLength(100)
            .IsRequired();

        // A user cannot have the same role granted twice
        builder.HasIndex(r => new { r.AppUserId, r.RoleName })
            .IsUnique()
            .HasDatabaseName("IX_AppUserRoles_UserId_Role");
    }
}

using BusBooking.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.EntraObjectId).HasMaxLength(36).IsRequired();
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.Address).HasMaxLength(500);
        builder.HasIndex(u => u.EntraObjectId).IsUnique();
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Ignore(u => u.DomainEvents);
    }
}

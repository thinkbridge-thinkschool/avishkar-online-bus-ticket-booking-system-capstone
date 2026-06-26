using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class LocalCredentialConfiguration : IEntityTypeConfiguration<LocalCredential>
{
    public void Configure(EntityTypeBuilder<LocalCredential> builder)
    {
        builder.ToTable("LocalCredentials");
        builder.HasKey(l => l.Id);

        // BCrypt output is always 60 characters
        builder.Property(l => l.PasswordHash)
            .HasMaxLength(60)
            .IsRequired();

        // Token hashes are SHA-256 outputs (64 hex chars)
        builder.Property(l => l.EmailVerificationTokenHash)
            .HasMaxLength(64);

        builder.Property(l => l.PasswordResetTokenHash)
            .HasMaxLength(64);

        // Enforce one credential record per user at the database level
        builder.HasIndex(l => l.AppUserId)
            .IsUnique()
            .HasDatabaseName("IX_LocalCredentials_AppUserId");
    }
}

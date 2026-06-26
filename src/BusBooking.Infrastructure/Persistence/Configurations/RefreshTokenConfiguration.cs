using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);

        // SHA-256 = 32 bytes = 64 hex characters
        builder.Property(t => t.TokenHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(t => t.TokenHash)
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(t => t.AppUserId)
            .HasDatabaseName("IX_RefreshTokens_AppUserId");

        // Self-referential FK for rotation audit chain — no cascade to avoid cycles
        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(t => t.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.NoAction)
            .IsRequired(false);
    }
}

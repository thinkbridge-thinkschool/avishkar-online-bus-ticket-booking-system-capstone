using BusBooking.Domain.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class AuthAuditLogConfiguration : IEntityTypeConfiguration<AuthAuditLog>
{
    public void Configure(EntityTypeBuilder<AuthAuditLog> builder)
    {
        builder.ToTable("AuthAuditLogs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(l => l.EventType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(l => l.IpAddress).HasMaxLength(45);   // IPv6 max = 39 chars
        builder.Property(l => l.UserAgent).HasMaxLength(512);

        builder.HasIndex(l => l.CreatedAt)
            .HasDatabaseName("IX_AuthAuditLogs_CreatedAt");

        builder.HasIndex(l => l.AppUserId)
            .HasDatabaseName("IX_AuthAuditLogs_AppUserId");

        builder.HasIndex(l => l.EventType)
            .HasDatabaseName("IX_AuthAuditLogs_EventType");
    }
}

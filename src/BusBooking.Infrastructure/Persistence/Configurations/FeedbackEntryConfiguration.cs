using BusBooking.Domain.Feedback.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class FeedbackEntryConfiguration : IEntityTypeConfiguration<FeedbackEntry>
{
    public void Configure(EntityTypeBuilder<FeedbackEntry> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Rating).IsRequired();
        builder.Property(f => f.Comment).HasMaxLength(1000).IsRequired();
        builder.Property(f => f.Category).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(f => f.BookingId).IsUnique();
        builder.HasIndex(f => f.UserId);
        builder.HasIndex(f => f.ScheduleId);
        builder.HasIndex(f => new { f.TenantId, f.ScheduleId });
        builder.Ignore(f => f.DomainEvents);
    }
}

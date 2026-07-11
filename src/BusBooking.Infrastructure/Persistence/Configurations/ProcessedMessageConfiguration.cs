using BusBooking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.HasKey(m => new { m.MessageId, m.SubscriptionName });
        builder.Property(m => m.MessageId).HasMaxLength(100);
        builder.Property(m => m.SubscriptionName).HasMaxLength(100);
    }
}

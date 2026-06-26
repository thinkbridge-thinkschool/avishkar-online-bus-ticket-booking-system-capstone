using BusBooking.Domain.Booking.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BusBooking.Infrastructure.Persistence.Configurations;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(p => p.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.TransactionReference).HasMaxLength(30);
        builder.Property(p => p.GatewayTransactionId).HasMaxLength(50);
        builder.HasIndex(p => p.BookingId).IsUnique();
        builder.HasIndex(p => p.TenantId);
        builder.Ignore(p => p.DomainEvents);
    }
}

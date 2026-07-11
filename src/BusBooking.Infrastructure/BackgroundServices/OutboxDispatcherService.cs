using System.Diagnostics;
using System.Text.Json;
using BusBooking.Application.Common;
using BusBooking.Domain.Booking.Events;
using BusBooking.Domain.Common;
using BusBooking.Domain.Tenants.Events;
using BusBooking.Domain.Vendor.Events;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.BackgroundServices;

// Dispatches OutboxMessage rows written by OutboxSavingChangesInterceptor to the real
// IEventPublisher (Service Bus), out-of-band from the request that raised them. This is
// what makes publishing reliable — a Service Bus outage no longer loses events, it just
// delays them until the next poll, retried up to MaxAttempts before being dead-lettered
// for manual inspection.
internal sealed class OutboxDispatcherService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxDispatcherService> logger) : BackgroundService
{
    private static readonly ActivitySource _source = new("BusBooking.Worker");
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);
    private const int BatchSize = 50;
    private const int MaxAttempts = 5;

    // JsonSerializer needs the concrete CLR type to deserialize into — IDomainEvent alone
    // isn't enough — so map the EventType string written by the interceptor back to a Type.
    private static readonly IReadOnlyDictionary<string, Type> EventTypesByName =
        new[]
        {
            typeof(BookingConfirmedEvent),
            typeof(BookingCancelledEvent),
            typeof(PaymentCompletedEvent),
            typeof(PaymentFailedEvent),
            typeof(TenantApprovedEvent),
            typeof(TenantSuspendedEvent),
            typeof(VendorApprovedEvent),
            typeof(VendorRejectedEvent),
        }.ToDictionary(t => t.Name);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Interval, stoppingToken);
            try
            {
                await DispatchPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "OutboxDispatcherService: failed to dispatch pending messages; will retry in {Interval}", Interval);
            }
        }
    }

    // Internal (not private) so tests can invoke a single dispatch cycle directly,
    // the same testability pattern used by SeatExpiryService/BookingCleanupService.
    internal async Task DispatchPendingAsync(CancellationToken ct)
    {
        using var activity = _source.StartActivity("OutboxDispatcherService.DispatchPending");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var pending = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && !m.DeadLettered)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        var dispatched = 0;
        var failed = 0;
        foreach (var message in pending)
        {
            try
            {
                await PublishAsync(publisher, message, ct);
                message.ProcessedAt = DateTime.UtcNow;
                dispatched++;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.Error = ex.Message;
                if (message.Attempts >= MaxAttempts)
                    message.DeadLettered = true;
                failed++;
            }
        }

        await db.SaveChangesAsync(ct);

        activity?.SetTag("outbox.dispatched", dispatched);
        activity?.SetTag("outbox.failed", failed);
        if (dispatched > 0 || failed > 0)
            logger.LogInformation(
                "OutboxDispatcherService: dispatched {Dispatched}, failed {Failed} (of {Total} pending)",
                dispatched, failed, pending.Count);
    }

    private static Task PublishAsync(IEventPublisher publisher, OutboxMessage message, CancellationToken ct)
    {
        if (!EventTypesByName.TryGetValue(message.EventType, out var eventType))
            throw new InvalidOperationException($"Unknown outbox event type '{message.EventType}'.");

        var evt = (IDomainEvent?)JsonSerializer.Deserialize(message.Payload, eventType)
            ?? throw new InvalidOperationException($"Failed to deserialize outbox payload for '{message.EventType}'.");

        return publisher.PublishAsync(evt, message.Id, ct);
    }
}

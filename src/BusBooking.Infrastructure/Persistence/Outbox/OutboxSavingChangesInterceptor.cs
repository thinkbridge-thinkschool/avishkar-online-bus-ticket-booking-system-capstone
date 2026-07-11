using System.Text.Json;
using BusBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BusBooking.Infrastructure.Persistence.Outbox;

// Scans the change tracker for entities with pending domain events and writes them as
// OutboxMessage rows in the SAME SaveChanges call that persists the business mutation —
// guaranteeing both commit or roll back together, atomically. Command handlers no longer
// publish directly; they just raise events (via aggregate methods) and save once. The
// OutboxDispatcherService is what actually calls IEventPublisher, out-of-band, later.
internal sealed class OutboxSavingChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            AppendOutboxMessages(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is not null)
            AppendOutboxMessages(eventData.Context);

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void AppendOutboxMessages(DbContext context)
    {
        var entitiesWithEvents = context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var evt in entity.DomainEvents)
            {
                context.Set<OutboxMessage>().Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    EventType = evt.GetType().Name,
                    Payload = JsonSerializer.Serialize(evt, evt.GetType()),
                    OccurredAt = DateTime.UtcNow,
                });
            }

            entity.ClearDomainEvents();
        }
    }
}

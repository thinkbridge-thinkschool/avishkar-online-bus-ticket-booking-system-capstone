using BusBooking.Domain.Common;

namespace BusBooking.Application.Common;

public interface IEventPublisher
{
    // messageId, when supplied, becomes the transport message's dedup key (e.g. Service Bus
    // MessageId) — the Outbox dispatcher passes its row's own Id so a retried publish of the
    // exact same event occurrence is deduplicated by the broker, not just by our own retry loop.
    Task PublishAsync<T>(T domainEvent, Guid? messageId = null, CancellationToken ct = default) where T : IDomainEvent;
}

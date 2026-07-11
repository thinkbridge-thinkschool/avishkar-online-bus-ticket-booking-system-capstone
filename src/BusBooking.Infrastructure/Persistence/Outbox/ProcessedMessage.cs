namespace BusBooking.Infrastructure.Persistence.Outbox;

// Inbox pattern: guards against Service Bus's at-least-once REDELIVERY (distinct from
// send-side duplicate detection on the topic) — a message can be fully processed and then
// redelivered if the CompleteMessageAsync acknowledgement itself is lost. Composite key
// (MessageId, SubscriptionName) since the same MessageId is legitimately reused across
// different subscriptions.
public sealed class ProcessedMessage
{
    public required string MessageId { get; init; }
    public required string SubscriptionName { get; init; }
    public required DateTime ProcessedAt { get; init; }
}

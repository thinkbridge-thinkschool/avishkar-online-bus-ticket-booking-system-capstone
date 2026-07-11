namespace BusBooking.Infrastructure.Persistence.Outbox;

// Infrastructure-only concern (message envelope + delivery bookkeeping for Service Bus),
// not a domain concept — deliberately not a BaseEntity/aggregate.
public sealed class OutboxMessage
{
    public required Guid Id { get; init; }
    public required string EventType { get; init; }
    public required string Payload { get; init; }
    public required DateTime OccurredAt { get; init; }
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? Error { get; set; }
    public bool DeadLettered { get; set; }
}

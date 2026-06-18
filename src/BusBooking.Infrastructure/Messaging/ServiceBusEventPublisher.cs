using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Common;
using BusBooking.Domain.Common;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Messaging;

internal sealed class ServiceBusEventPublisher(
    ServiceBusClient client,
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher
{
    private static readonly ActivitySource _source = new("BusBooking.Messaging");

    // Topic name derived from event type: BookingConfirmedEvent → "booking-confirmed"
    private static string TopicFor(Type eventType) =>
        string.Concat(eventType.Name
            .Replace("Event", "")
            .Select((c, i) => i > 0 && char.IsUpper(c) ? $"-{c}" : $"{c}"))
            .ToLowerInvariant();

    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent
    {
        var topic = TopicFor(typeof(T));

        using var activity = _source.StartActivity($"ServiceBus.Publish {topic}");
        activity?.SetTag("messaging.system", "servicebus");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.event_type", typeof(T).Name);

        await using var sender = client.CreateSender(topic);

        var body = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = typeof(T).Name,
        };

        // Propagate W3C trace context so a downstream consumer can stitch the trace.
        if (Activity.Current is { } current)
        {
            message.ApplicationProperties["traceparent"] = current.Id;
            message.ApplicationProperties["tracestate"] = current.TraceStateString ?? string.Empty;
        }

        await sender.SendMessageAsync(message, ct);
        logger.LogInformation(
            "Published {EventType} to topic {Topic} | TraceId={TraceId}",
            typeof(T).Name, topic, Activity.Current?.TraceId.ToString() ?? "none");
    }
}

using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Common;
using BusBooking.Domain.Common;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;

namespace BusBooking.Infrastructure.Messaging;

internal sealed class ServiceBusEventPublisher(
    ServiceBusClient client,
    ResiliencePipelineProvider<string> resilienceProvider,
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher
{
    private static readonly ActivitySource _source = new("BusBooking.Messaging");

    // Topic name derived from event type: BookingConfirmedEvent → "booking-confirmed"
    private static string TopicFor(Type eventType) =>
        string.Concat(eventType.Name
            .Replace("Event", "")
            .Select((c, i) => i > 0 && char.IsUpper(c) ? $"-{c}" : $"{c}"))
            .ToLowerInvariant();

    public async Task PublishAsync<T>(T domainEvent, Guid? messageId = null, CancellationToken ct = default) where T : IDomainEvent
    {
        // Use the runtime concrete type, not typeof(T) — T resolves to IDomainEvent when the
        // caller iterates IReadOnlyCollection<IDomainEvent>, which would produce topic "i-domain".
        var eventType = domainEvent.GetType();
        var topic = TopicFor(eventType);

        using var activity = _source.StartActivity($"ServiceBus.Publish {topic}");
        activity?.SetTag("messaging.system", "servicebus");
        activity?.SetTag("messaging.destination", topic);
        activity?.SetTag("messaging.operation", "publish");
        activity?.SetTag("messaging.event_type", eventType.Name);

        await using var sender = client.CreateSender(topic);

        var body = JsonSerializer.Serialize(domainEvent, eventType);
        var message = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = eventType.Name,
            // Lets the topic's duplicate-detection window dedup a retried publish of the exact
            // same outbox row (same messageId) without relying solely on our own retry logic.
            MessageId = messageId?.ToString(),
        };

        // Propagate W3C trace context so a downstream consumer can stitch the trace.
        if (Activity.Current is { } current)
        {
            message.ApplicationProperties["traceparent"] = current.Id;
            message.ApplicationProperties["tracestate"] = current.TraceStateString ?? string.Empty;
        }

        var pipeline = resilienceProvider.GetPipeline("service-bus-publish");

        try
        {
            await pipeline.ExecuteAsync(
                async token => await sender.SendMessageAsync(message, token), ct);
            logger.LogInformation(
                "Published {EventType} to topic {Topic} | TraceId={TraceId}",
                eventType.Name, topic, Activity.Current?.TraceId.ToString() ?? "none");
        }
        catch (ServiceBusException ex)
        {
            // No longer swallowed: this is called from OutboxDispatcherService, out-of-band
            // from the original request — there is no user-facing operation left to protect
            // from this failure. Letting it propagate lets the dispatcher retry the same
            // outbox row on its next poll instead of silently losing the event.
            logger.LogError(ex,
                "Failed to publish {EventType} to Service Bus topic {Topic}", eventType.Name, topic);
            throw;
        }
    }
}

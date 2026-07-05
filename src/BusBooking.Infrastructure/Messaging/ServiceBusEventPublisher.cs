using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Common;
using BusBooking.Domain.Common;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Messaging;

internal sealed class ServiceBusEventPublisher(
    ServiceBusClient client,      // connect Azure Service Bus.
    ILogger<ServiceBusEventPublisher> logger) : IEventPublisher // Used to write logs, BookingConfirmedEvent Published
{
    private static readonly ActivitySource _source = new("BusBooking.Messaging"); // ActivitySource measures: Time taken

    // Topic name derived from event type: BookingConfirmedEvent → "booking-confirmed"
    private static string TopicFor(Type eventType) =>
        string.Concat(eventType.Name
            .Replace("Event", "")
            .Select((c, i) => i > 0 && char.IsUpper(c) ? $"-{c}" : $"{c}"))
            .ToLowerInvariant();

    public async Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent
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
        };

        // Propagate W3C trace context so a downstream consumer can stitch the trace.
        if (Activity.Current is { } current)
        {
            message.ApplicationProperties["traceparent"] = current.Id;
            message.ApplicationProperties["tracestate"] = current.TraceStateString ?? string.Empty;
        }

        try
        {
            await sender.SendMessageAsync(message, ct);
            logger.LogInformation(
                "Published {EventType} to topic {Topic} | TraceId={TraceId}",
                eventType.Name, topic, Activity.Current?.TraceId.ToString() ?? "none");
        }
        catch (ServiceBusException ex)
        {
            // Payment is already committed to the database — a Service Bus failure must not
            // surface as a payment error to the user. Log for Application Insights alerting.
            logger.LogError(ex,
                "Failed to publish {EventType} to Service Bus topic {Topic} — event lost, payment was committed",
                eventType.Name, topic);
        }
    }
}

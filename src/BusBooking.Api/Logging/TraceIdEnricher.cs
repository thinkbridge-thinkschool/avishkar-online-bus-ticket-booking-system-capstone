using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace BusBooking.Api.Logging;

// Correlation ID = the existing W3C trace context (Activity.Current), not a new mechanism —
// the same value the exception handler already returns as "traceId" and ServiceBusEventPublisher
// already propagates as "traceparent". This just taps into it so console log lines, error
// responses, and Application Insights traces all key on the identical ID.
internal sealed class TraceIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var traceId = Activity.Current?.Id;
        if (traceId is not null)
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", traceId));
    }
}

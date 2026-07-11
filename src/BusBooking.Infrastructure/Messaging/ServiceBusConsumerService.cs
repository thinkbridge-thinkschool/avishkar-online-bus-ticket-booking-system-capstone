using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using BusBooking.Application.Common.Interfaces;
using BusBooking.Domain.Booking.Events;
using BusBooking.Infrastructure.Persistence;
using BusBooking.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BusBooking.Infrastructure.Messaging;

// Reads booking-confirmed/booking-cancelled messages and sends the corresponding
// notification email — the first real implementation of that notification (nothing sent
// one synchronously anywhere before this). Idempotent via the ProcessedMessage Inbox table:
// Service Bus's at-least-once delivery can redeliver a message whose processing already
// succeeded but whose own CompleteMessageAsync acknowledgement was lost in transit.
internal sealed class ServiceBusConsumerService(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceBusConsumerService> logger) : BackgroundService
{
    private static readonly ActivitySource _source = new("BusBooking.Messaging");
    internal const string TopicConfirmed = "booking-confirmed";
    internal const string TopicCancelled = "booking-cancelled";
    internal const string SubscriptionConfirmed = "sub-booking-confirmed";
    internal const string SubscriptionCancelled = "sub-booking-cancelled";

    private ServiceBusProcessor? _confirmedProcessor;
    private ServiceBusProcessor? _cancelledProcessor;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _confirmedProcessor = client.CreateProcessor(TopicConfirmed, SubscriptionConfirmed);
        _confirmedProcessor.ProcessMessageAsync += OnConfirmedMessageAsync;
        _confirmedProcessor.ProcessErrorAsync += OnProcessErrorAsync;

        _cancelledProcessor = client.CreateProcessor(TopicCancelled, SubscriptionCancelled);
        _cancelledProcessor.ProcessMessageAsync += OnCancelledMessageAsync;
        _cancelledProcessor.ProcessErrorAsync += OnProcessErrorAsync;

        await _confirmedProcessor.StartProcessingAsync(cancellationToken);
        await _cancelledProcessor.StartProcessingAsync(cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    // Message pumping is driven by the two ServiceBusProcessors started above, not a loop —
    // nothing for the BackgroundService's own execute loop to do.
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_confirmedProcessor is not null)
            await _confirmedProcessor.StopProcessingAsync(cancellationToken);
        if (_cancelledProcessor is not null)
            await _cancelledProcessor.StopProcessingAsync(cancellationToken);

        await base.StopAsync(cancellationToken);
    }

    private async Task OnConfirmedMessageAsync(ProcessMessageEventArgs args)
    {
        await HandleMessageAsync(
            args.Message.MessageId, args.Message.Subject, args.Message.Body.ToString(),
            SubscriptionConfirmed, args.CancellationToken);
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    private async Task OnCancelledMessageAsync(ProcessMessageEventArgs args)
    {
        await HandleMessageAsync(
            args.Message.MessageId, args.Message.Subject, args.Message.Body.ToString(),
            SubscriptionCancelled, args.CancellationToken);
        await args.CompleteMessageAsync(args.Message, args.CancellationToken);
    }

    // Internal (not private) so tests can drive the idempotent-processing logic directly,
    // without needing a live ServiceBusReceiver to build a real ProcessMessageEventArgs.
    internal async Task HandleMessageAsync(
        string messageId, string subject, string body, string subscriptionName, CancellationToken ct)
    {
        using var activity = _source.StartActivity($"ServiceBus.Process {subscriptionName}");
        activity?.SetTag("messaging.system", "servicebus");
        activity?.SetTag("messaging.operation", "process");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BusBookingDbContext>();

        var alreadyProcessed = await db.ProcessedMessages
            .AnyAsync(m => m.MessageId == messageId && m.SubscriptionName == subscriptionName, ct);
        if (alreadyProcessed)
        {
            logger.LogInformation(
                "ServiceBusConsumerService: message {MessageId} already processed for {Subscription}, skipping",
                messageId, subscriptionName);
            return;
        }

        var eventType = subject switch
        {
            nameof(BookingConfirmedEvent) => typeof(BookingConfirmedEvent),
            nameof(BookingCancelledEvent) => typeof(BookingCancelledEvent),
            _ => throw new InvalidOperationException($"Unknown message subject '{subject}'."),
        };
        var evt = JsonSerializer.Deserialize(body, eventType)
            ?? throw new InvalidOperationException($"Failed to deserialize message body for '{subject}'.");

        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        switch (evt)
        {
            case BookingConfirmedEvent e:
                await emailService.SendBookingConfirmationAsync(
                    e.UserEmail, e.UserName, e.BookingId, e.SeatNumbers, e.TotalAmount, ct);
                break;
            case BookingCancelledEvent e:
                await emailService.SendBookingCancellationAsync(
                    e.UserEmail, e.BookingId, e.ReleasedSeatNumbers, ct);
                break;
        }

        db.ProcessedMessages.Add(new ProcessedMessage
        {
            MessageId = messageId,
            SubscriptionName = subscriptionName,
            ProcessedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "ServiceBusConsumerService: processed {MessageId} from {Subscription}", messageId, subscriptionName);
    }

    private Task OnProcessErrorAsync(ProcessErrorEventArgs args)
    {
        logger.LogError(args.Exception,
            "ServiceBusConsumerService: error processing message from {EntityPath}", args.EntityPath);
        return Task.CompletedTask;
    }
}

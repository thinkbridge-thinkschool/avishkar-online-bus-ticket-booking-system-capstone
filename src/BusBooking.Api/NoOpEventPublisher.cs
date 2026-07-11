using BusBooking.Application.Common;
using BusBooking.Domain.Common;
using Microsoft.Extensions.Logging;

namespace BusBooking.Api;

public sealed class NoOpEventPublisher(ILogger<NoOpEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(T domainEvent, Guid? messageId = null, CancellationToken ct = default) where T : IDomainEvent
    {
        logger.LogInformation("[NoOp] Would publish {Event}: {Payload}", typeof(T).Name, domainEvent);
        return Task.CompletedTask;
    }
}

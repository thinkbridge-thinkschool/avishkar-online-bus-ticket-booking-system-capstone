using BusBooking.Application.Common;
using BusBooking.Domain.Common;

namespace BusBooking.Application.Tests.Fakes;

public sealed class FakeEventPublisher : IEventPublisher
{
    private readonly List<IDomainEvent> _published = [];

    public Task PublishAsync<T>(T domainEvent, CancellationToken ct = default) where T : IDomainEvent
    {
        _published.Add(domainEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyList<IDomainEvent> Published => _published.AsReadOnly();
}

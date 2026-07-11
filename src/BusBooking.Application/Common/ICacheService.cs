namespace BusBooking.Application.Common;

// Ports-and-adapters port mirroring IEventPublisher: Application depends only on this
// abstraction (it has zero external package references today), Infrastructure supplies the
// real HybridCache-backed implementation.
public interface ICacheService
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default);

    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}

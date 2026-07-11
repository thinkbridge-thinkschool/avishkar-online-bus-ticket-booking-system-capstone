using BusBooking.Application.Common;

namespace BusBooking.Application.Tests.Fakes;

// Pass-through — always invokes the factory, never actually caches. Handler unit tests care
// about the repository/business-logic interaction, not caching behavior (that's covered by
// HybridCache-specific integration tests).
public sealed class FakeCacheService : ICacheService
{
    public int RemoveByTagCallCount { get; private set; }

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default) =>
        await factory(ct);

    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default)
    {
        RemoveByTagCallCount++;
        return ValueTask.CompletedTask;
    }
}

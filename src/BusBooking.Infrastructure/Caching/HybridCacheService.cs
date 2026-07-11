using BusBooking.Application.Common;
using Microsoft.Extensions.Caching.Hybrid;

namespace BusBooking.Infrastructure.Caching;

internal sealed class HybridCacheService(HybridCache cache) : ICacheService
{
    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default)
    {
        var options = expiration is null ? null : new HybridCacheEntryOptions { Expiration = expiration };
        return await cache.GetOrCreateAsync(
            key,
            async token => await factory(token),
            options,
            tags,
            ct);
    }

    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default) =>
        cache.RemoveByTagAsync(tag, ct);
}

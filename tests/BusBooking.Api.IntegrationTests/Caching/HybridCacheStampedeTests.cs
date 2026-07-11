using BusBooking.Application.Common;
using BusBooking.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace BusBooking.Api.IntegrationTests.Caching;

public sealed class HybridCacheStampedeTests
{
    private static ICacheService BuildCacheService()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        services.AddSingleton<ICacheService, HybridCacheService>();
        return services.BuildServiceProvider().GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetOrCreateAsync_ConcurrentCallsForSameKey_InvokesFactoryOnlyOnce()
    {
        // Proves HybridCache's built-in stampede protection: concurrent callers for a key that
        // misses cache all await the SAME in-flight factory invocation, rather than each
        // independently re-running the (expensive) factory — the actual mechanism behind
        // "stampede protection", nothing hand-built here.
        var cache = BuildCacheService();
        var callCount = 0;

        var tasks = Enumerable.Range(0, 20).Select(_ => cache.GetOrCreateAsync(
            "stampede-key",
            async ct =>
            {
                Interlocked.Increment(ref callCount);
                await Task.Delay(200, ct);
                return 42;
            }).AsTask());

        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, callCount);
        Assert.All(results, r => Assert.Equal(42, r));
    }

    [Fact]
    public async Task RemoveByTagAsync_InvalidatesEntriesWithThatTag()
    {
        var cache = BuildCacheService();
        var callCount = 0;

        Task<int> Factory(CancellationToken ct)
        {
            callCount++;
            return Task.FromResult(callCount);
        }

        var first = await cache.GetOrCreateAsync("tagged-key", Factory, tags: ["my-tag"]);
        var second = await cache.GetOrCreateAsync("tagged-key", Factory, tags: ["my-tag"]);
        Assert.Equal(first, second); // second call was a cache hit — factory not re-invoked
        Assert.Equal(1, callCount);

        await cache.RemoveByTagAsync("my-tag");

        var third = await cache.GetOrCreateAsync("tagged-key", Factory, tags: ["my-tag"]);
        Assert.Equal(2, callCount); // factory re-invoked after invalidation
        Assert.NotEqual(second, third);
    }
}

using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.AspNetCore.Impl;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

public class CachingAccessResolverTests
{
    private static readonly AccessSubject Subject = AccessSubject.Create("u1");

    private static ServiceProvider BuildProvider(CountingAccessStore store, TimeSpan? lifetime)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(options => options.CacheLifetime = lifetime);
        services.AddAccessStore(store);

        return services.BuildServiceProvider(validateScopes: true);
    }

    [Fact]
    public async Task ResolveAsync_NoLifetimeConfigured_AsksTheStoreEveryTime()
    {
        // Off by default, because a cached grant outlives a revocation by up to the lifetime.
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, null);
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IAccessResolver>();

        await resolver.ResolveAsync(Subject, Ct.Token);
        await resolver.ResolveAsync(Subject, Ct.Token);

        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ResolveAsync_LifetimeConfigured_AsksTheStoreOnce()
    {
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, TimeSpan.FromMinutes(3));
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IAccessResolver>();

        var first = await resolver.ResolveAsync(Subject, Ct.Token);
        var second = await resolver.ResolveAsync(Subject, Ct.Token);

        Assert.Equal(1, store.Calls);
        Assert.Equal(first, second);
    }

    [Fact]
    public async Task ResolveAsync_ZeroLifetime_DoesNotCache()
    {
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, TimeSpan.Zero);
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IAccessResolver>();

        await resolver.ResolveAsync(Subject, Ct.Token);
        await resolver.ResolveAsync(Subject, Ct.Token);

        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ResolveAsync_DifferentSubjects_DoNotShareAnEntry()
    {
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, TimeSpan.FromMinutes(3));
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IAccessResolver>();

        await resolver.ResolveAsync(AccessSubject.Create("u1"), Ct.Token);
        await resolver.ResolveAsync(AccessSubject.Create("u2"), Ct.Token);

        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ResolveAsync_SameIdInDifferentOrganisations_DoNotShareAnEntry()
    {
        // The cache key covers the whole composite identity, not just the id.
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, TimeSpan.FromMinutes(3));
        using var scope = provider.CreateScope();

        var resolver = scope.ServiceProvider.GetRequiredService<IAccessResolver>();

        await resolver.ResolveAsync(AccessSubject.Create("u1", new Dictionary<string, string> { ["organization"] = "acme" }), Ct.Token);
        await resolver.ResolveAsync(AccessSubject.Create("u1", new Dictionary<string, string> { ["organization"] = "other" }), Ct.Token);

        Assert.Equal(2, store.Calls);
    }

    [Fact]
    public async Task ResolveAsync_Always_NamespacesItsCacheEntries()
    {
        // The predecessor's key was "eff-acc-{userId}" in the host's shared cache, which anything
        // else using that cache could collide with.
        var store = new CountingAccessStore("a");

        using var provider = BuildProvider(store, TimeSpan.FromMinutes(3));
        using var scope = provider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IAccessResolver>().ResolveAsync(Subject, Ct.Token);

        var cache = (MemoryCache)provider.GetRequiredService<IMemoryCache>();
        var keys = cache.Keys.OfType<string>().ToList();

        Assert.All(keys, key => Assert.StartsWith("ApricotFramework.AccessAuthorization.", key, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ResolveAsync_CachedSet_IsStillReachedThroughTheRulePipeline()
    {
        var store = new CountingAccessStore("sales:orders:read");

        using var provider = BuildProvider(store, TimeSpan.FromMinutes(3));
        using var scope = provider.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAccessAuthorization>();
        var context = AccessContext.For(Subject);

        Assert.True(await authorization.CheckAnyAsync(context, ["sales:orders:read"], Ct.Token));
        Assert.True(await authorization.CheckAnyAsync(context, ["sales:orders:read"], Ct.Token));
        Assert.Equal(1, store.Calls);
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());

        Assert.Throws<ArgumentNullException>(() => new CachingAccessResolver(null!, cache, null!));
    }
}

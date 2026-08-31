using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.AspNetCore.Impl;
using ApricotFramework.AccessAuthorization.AspNetCore.Subjects;
using ApricotFramework.AccessAuthorization.Extensions;
using ApricotFramework.AccessAuthorization.Impl;
using ApricotFramework.AccessAuthorization.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

public class AccessAuthorizationServiceCollectionExtensionsTests
{
    private static readonly AccessContext Context = AccessContext.For(AccessSubject.Create("u1"));

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    [Fact]
    public void AddAccessAuthorization_WithConfiguration_ResolvesTheDecisionPoint()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(CreateConfiguration());

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.IsType<DefaultAccessAuthorization>(scope.ServiceProvider.GetRequiredService<IAccessAuthorization>());
    }

    [Fact]
    public void AddAccessAuthorization_Always_RegistersTheDecisionPointAsScoped()
    {
        // Scoped, because a store normally holds a connection or a unit of work. The predecessor
        // forced every store to be a singleton.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessStore<ScopedStore>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var first = provider.CreateScope();
        using var second = provider.CreateScope();

        Assert.NotSame(
            first.ServiceProvider.GetRequiredService<IAccessAuthorization>(),
            second.ServiceProvider.GetRequiredService<IAccessAuthorization>());
    }

    [Fact]
    public async Task AddAccessStore_SeveralStores_AreAllConsulted()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessStore(new CountingAccessStore("a"));
        services.AddAccessStore(new CountingAccessStore("b"));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAccessAuthorization>();

        Assert.True(await authorization.CheckAllAsync(Context, ["a", "b"], Ct.Token));
    }

    [Fact]
    public void AddAccessStore_SameTypeTwice_IsRegisteredOnce()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessStore<ScopedStore>();
        services.AddAccessStore<ScopedStore>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.Single(scope.ServiceProvider.GetServices<IAccessStore>());
    }

    [Fact]
    public async Task AddAccessAuthorization_NoStores_DeniesEverything()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAccessAuthorization>();

        Assert.False(await authorization.CheckAnyAsync(Context, ["a"], Ct.Token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AddAccessBypassRule_EitherSideOfAddAccessAuthorization_StillRunsFirst(bool beforeCore)
    {
        // Stage membership cannot come from service registration order, or the same two lines would
        // mean different things depending on which was typed first.
        var store = new CountingAccessStore();
        var services = new ServiceCollection();

        services.AddLogging();

        if (beforeCore)
        {
            services.AddAccessBypassRule<AllowAllRule>();
            services.AddAccessAuthorization();
        }
        else
        {
            services.AddAccessAuthorization();
            services.AddAccessBypassRule<AllowAllRule>();
        }

        services.AddAccessStore(store);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var allowed = await scope.ServiceProvider.GetRequiredService<IAccessAuthorization>().CheckAnyAsync(Context, ["a"], Ct.Token);

        Assert.True(allowed);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task AddAccessRule_AfterTheAssignedLookup_CannotOverrideAnEarlierDeny()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessBypassRule<DenyAllRule>();
        services.AddAccessRule<AllowAllRule>();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.False(await scope.ServiceProvider.GetRequiredService<IAccessAuthorization>().CheckAnyAsync(Context, ["a"], Ct.Token));
    }

    [Fact]
    public async Task AddAccessRule_AfterTheAssignedLookup_CanGrantWhatIsNotAssigned()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessRule<AllowAllRule>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.True(await scope.ServiceProvider.GetRequiredService<IAccessAuthorization>().CheckAnyAsync(Context, ["a"], Ct.Token));
    }

    [Fact]
    public async Task AddAccessAuthorization_ConsumerRegisteredDecisionPointFirst_Wins()
    {
        // The decision point is the seam a remote one slots into: register it and everything above
        // keeps working while nothing below it is consulted.
        var store = new CountingAccessStore("a");
        var stub = new StubAccessAuthorization("b");
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IAccessAuthorization>(stub);
        services.AddAccessAuthorization();
        services.AddAccessStore(store);
        services.AddAccessRule<AllowAllRule>();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var authorization = scope.ServiceProvider.GetRequiredService<IAccessAuthorization>();

        Assert.Same(stub, authorization);
        Assert.False(await authorization.CheckAnyAsync(Context, ["a"], Ct.Token));
        Assert.True(await authorization.CheckAnyAsync(Context, ["b"], Ct.Token));
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public void AddAccessAuthorization_Always_RegistersBothAuthorizationHandlers()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        // One for accesses, one for scopes. Both arrive together so a scope attribute can never
        // contribute a requirement that nothing is able to satisfy.
        Assert.Equal(2, scope.ServiceProvider.GetServices<IAuthorizationHandler>().Count());
    }

    [Fact]
    public void AddAccessAuthorization_CalledTwice_StillResolves()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAccessAuthorization>());
        Assert.Equal(2, scope.ServiceProvider.GetServices<IAuthorizationHandler>().Count());
    }

    [Fact]
    public void AddAccessAuthorization_WithConfiguration_BindsCacheLifetimeAndSubjectClaims()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(CreateConfiguration(
            ("Authorization:CacheLifetime", "00:02:30"),
            ("Authorization:Subject:IdClaimTypes:0", "user_id"),
            ("Authorization:Subject:AttributeClaims:org_id", "organization")));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var options = provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value;

        Assert.Equal(TimeSpan.FromSeconds(150), options.CacheLifetime);
        Assert.Contains("user_id", options.Subject.IdClaimTypes);
        Assert.Equal("organization", options.Subject.AttributeClaims["org_id"]);
    }

    [Fact]
    public void AddAccessAuthorization_CustomSectionName_BindsThatSection()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(CreateConfiguration(("Custom:CacheLifetime", "00:00:30")), "Custom");

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(TimeSpan.FromSeconds(30), provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value.CacheLifetime);
    }

    [Fact]
    public void AddAccessAuthorization_NegativeCacheLifetime_FailsValidation()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(options => options.CacheLifetime = TimeSpan.FromSeconds(-1));

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value);
    }

    [Fact]
    public void AddAccessAuthorization_Always_ResolvesTheCachingResolver()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        // Always composed, but inert until a lifetime is configured, so turning caching on is a
        // settings change rather than a wiring change.
        Assert.IsType<CachingAccessResolver>(scope.ServiceProvider.GetRequiredService<IAccessResolver>());
    }

    [Fact]
    public void AddAccessAuthorization_Always_RegistersTheSubjectResolverAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsType<ClaimsAccessSubjectResolver>(provider.GetRequiredService<IAccessSubjectResolver>());
        Assert.Same(provider.GetRequiredService<IAccessSubjectResolver>(), provider.GetRequiredService<IAccessSubjectResolver>());
    }

    [Fact]
    public void AddAccessCatalog_FromStrings_ResolvesTheCatalog()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessCatalog(["a", "b"]);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Equal(2, provider.GetRequiredService<IAccessCatalog>().GetDefinitions().Count);
    }

    [Fact]
    public void AddAccessAuthorization_NullArguments_Throw()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddAccessAuthorization());
        Assert.Throws<ArgumentNullException>(() => services.AddAccessAuthorization((IConfiguration)null!));
        Assert.Throws<ArgumentNullException>(() => services.AddAccessAuthorization((Action<AccessAuthorizationOptions>)null!));
        Assert.Throws<ArgumentException>(() => services.AddAccessAuthorization(CreateConfiguration(), "  "));
    }

    /// <summary>
    /// A store with a scoped dependency, so a lifetime mistake fails scope validation.
    /// </summary>
    private sealed class ScopedStore : IAccessStore
    {
        public Task<IReadOnlySet<string>> GetAccessesAsync(AccessSubject subject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<string>>(new HashSet<string>(StringComparer.Ordinal));
        }
    }
}

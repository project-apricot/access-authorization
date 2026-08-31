using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Impl;
using ApricotFramework.AccessAuthorization.Options;
using ApricotFramework.AccessAuthorization.Scopes;
using ApricotFramework.AccessAuthorization.Scopes.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

/// <summary>
/// Scope authorization arrives with the one registration call. It used to need a second, and omitting
/// that second call produced a requirement no handler could satisfy — every scoped endpoint refused a
/// perfectly valid caller, with nothing anywhere saying why.
/// </summary>
public class ScopeRegistrationTests
{
    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
    }

    [Fact]
    public void AddAccessAuthorization_Always_BringsScopeAuthorizationWithIt()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.IsType<OptionsAwareScopeAuthorization>(provider.GetRequiredService<IScopeAuthorization>());
    }

    [Fact]
    public void AddAccessAuthorization_Always_RegistersBothHandlersExactlyOnce()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        Assert.Equal(2, scope.ServiceProvider.GetServices<IAuthorizationHandler>().Count());
    }

    [Fact]
    public void AddAccessAuthorization_WithConfiguration_BindsScopeSettingsFromTheSubsection()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(CreateConfiguration(
            ("Authorization:Scope:ClaimTypes:0", "permissions"),
            ("Authorization:Scope:Separators", " ,"),
            ("Authorization:CacheLifetime", "00:01:00")));

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var options = provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value;

        Assert.Contains("permissions", options.Scope.ClaimTypes);
        Assert.Equal(" ,", options.Scope.Separators);
        Assert.Equal(TimeSpan.FromMinutes(1), options.CacheLifetime);
    }

    [Fact]
    public void AddAccessAuthorization_ConfigurationReloaded_ChangesScopeReadingWithoutARestart()
    {
        var source = new Microsoft.Extensions.Configuration.Memory.MemoryConfigurationSource
        {
            InitialData = [new KeyValuePair<string, string?>("Authorization:Scope:ClaimTypes:0", "scope")],
        };

        var configuration = new ConfigurationBuilder().Add(source).Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(configuration);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var authorization = provider.GetRequiredService<IScopeAuthorization>();
        var principal = ClaimsPrincipalOrNone.User(("permissions", "billing.read")).Principal;

        Assert.False(authorization.CheckAny(principal, ["billing.read"]));

        configuration["Authorization:Scope:ClaimTypes:0"] = "permissions";
        configuration.Reload();

        Assert.True(authorization.CheckAny(principal, ["billing.read"]));
    }

    [Fact]
    public void AddAccessAuthorization_EmptyScopeClaimTypes_FailsValidation()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(options => options.Scope.ClaimTypes.Clear());

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value);
    }

    [Fact]
    public void AddAccessAuthorization_EmptyScopeSeparators_FailsValidation()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization(options => options.Scope.Separators = string.Empty);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<AccessAuthorizationOptions>>().Value);
    }

    [Fact]
    public void AddAccessAuthorization_ScopesUnused_DoesNotRequireAnyAccessWiring()
    {
        // A service that only ever asks scope questions needs no store, no catalog and no rules; the
        // access half simply never runs.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var authorization = provider.GetRequiredService<IScopeAuthorization>();

        Assert.True(authorization.CheckAll(ClaimsPrincipalOrNone.User(("scope", "billing.read")).Principal, ["billing.read"]));
        Assert.Empty(provider.GetServices<IAccessStore>());
    }
}

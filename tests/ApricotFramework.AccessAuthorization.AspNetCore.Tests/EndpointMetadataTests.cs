using ApricotFramework.AccessAuthorization.AspNetCore.Attributes;
using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

/// <summary>
/// The attributes carry their requirements as endpoint metadata and are composed by the authorization
/// middleware, not by an MVC filter. That is the entire reason one attribute governs a controller
/// action, a minimal-API endpoint and a gRPC method alike, so it is driven through the real
/// middleware rather than asserted structurally.
/// </summary>
public class EndpointMetadataTests
{
    private const int Ok = 200;

    private const int Challenged = 401;

    private const int Forbidden = 403;

    private static ServiceProvider BuildProvider(params string[] assigned)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();
        services.AddAccessAuthorization();
        services.AddAccessStore(new CountingAccessStore(assigned));

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Runs the authorization middleware over an endpoint carrying the given metadata.
    /// </summary>
    private static async Task<int> InvokeAsync(ServiceProvider provider, ClaimsPrincipalOrNone caller, params object[] metadata)
    {
        using var scope = provider.CreateScope();

        var reached = false;
        var middleware = new AuthorizationMiddleware(
            _ =>
            {
                reached = true;

                return Task.CompletedTask;
            },
            scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = caller.Principal,
        };

        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(metadata), "test"));

        await middleware.Invoke(httpContext);

        return reached ? Ok : httpContext.Response.StatusCode;
    }

    [Fact]
    public async Task Invoke_AssignedAccess_ReachesTheEndpoint()
    {
        using var provider = BuildProvider("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("sub", "u1")), new AuthorizeAnyAccessAttribute("sales:orders:read"));

        Assert.Equal(Ok, status);
    }

    [Fact]
    public async Task Invoke_UnassignedAccess_IsForbidden()
    {
        using var provider = BuildProvider("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("sub", "u1")), new AuthorizeAnyAccessAttribute("sales:orders:edit"));

        Assert.Equal(Forbidden, status);
    }

    [Fact]
    public async Task Invoke_AllAccessesRequiredAndOneMissing_IsForbidden()
    {
        using var provider = BuildProvider("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("sub", "u1")), new AuthorizeAllAccessAttribute("sales:orders:read", "sales:orders:edit"));

        Assert.Equal(Forbidden, status);
    }

    [Fact]
    public async Task Invoke_AnonymousCaller_IsChallengedRatherThanForbidden()
    {
        // A caller who never presented a token is asked to, rather than told no: 401 tells a client
        // to authenticate, where 403 says the answer will not change.
        using var provider = BuildProvider("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.Anonymous(), new AuthorizeAnyAccessAttribute("sales:orders:read"));

        Assert.Equal(Challenged, status);
    }

    [Fact]
    public async Task Invoke_AllowAnonymousAlongsideTheAttribute_ReachesTheEndpoint()
    {
        // Honoured by the middleware, so this library carries no equivalent of the predecessor's
        // manual allow-anonymous check.
        using var provider = BuildProvider();

        var status = await InvokeAsync(
            provider,
            ClaimsPrincipalOrNone.Anonymous(),
            new AuthorizeAnyAccessAttribute("sales:orders:read"),
            new AllowAnonymousAttribute());

        Assert.Equal(Ok, status);
    }

    [Fact]
    public async Task Invoke_SubjectWithoutAnIdClaim_IsForbidden()
    {
        // Authenticated but carrying no subject: the predecessor passed the resulting null straight
        // into the data store.
        using var provider = BuildProvider("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("client_id", "svc")), new AuthorizeAnyAccessAttribute("sales:orders:read"));

        Assert.Equal(Forbidden, status);
    }

    [Fact]
    public async Task Invoke_ScopedMachineTokenWithNoSubject_ReachesTheEndpoint()
    {
        // A client-credentials token has scopes and no subject, which is what scope authorization is
        // for. It must not be dragged into needing one.
        using var provider = BuildProvider();

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("scope", "billing.read billing.write")), new AuthorizeAllScopesAttribute("billing.read"));

        Assert.Equal(Ok, status);
    }

    [Fact]
    public async Task Invoke_MissingScope_IsForbidden()
    {
        using var provider = BuildProvider();

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("scope", "billing.read")), new AuthorizeAllScopesAttribute("billing.write"));

        Assert.Equal(Forbidden, status);
    }

    [Fact]
    public async Task Invoke_AccessAndScopeAttributesTogether_RequiresBoth()
    {
        using var provider = BuildProvider("sales:orders:read");

        var withBoth = await InvokeAsync(
            provider,
            ClaimsPrincipalOrNone.User(("sub", "u1"), ("scope", "sales.read")),
            new AuthorizeAnyAccessAttribute("sales:orders:read"),
            new AuthorizeAllScopesAttribute("sales.read"));

        var withoutScope = await InvokeAsync(
            provider,
            ClaimsPrincipalOrNone.User(("sub", "u1")),
            new AuthorizeAnyAccessAttribute("sales:orders:read"),
            new AuthorizeAllScopesAttribute("sales.read"));

        Assert.Equal(Ok, withBoth);
        Assert.Equal(Forbidden, withoutScope);
    }

    [Fact]
    public async Task Invoke_BypassRule_GrantsWithoutConsultingTheStore()
    {
        var store = new CountingAccessStore();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();
        services.AddAccessBypassRule<AllowAllRule>();
        services.AddAccessAuthorization();
        services.AddAccessStore(store);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("sub", "root")), new AuthorizeAnyAccessAttribute("sales:orders:read"));

        Assert.Equal(Ok, status);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task Invoke_EndpointConventionInsteadOfAttribute_BehavesIdentically()
    {
        using var provider = BuildProvider("sales:orders:read");

        var builder = new TestConventionBuilder();

        builder.RequireAccessAny("sales:orders:read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("sub", "u1")), [.. builder.BuildMetadata()]);

        Assert.Equal(Ok, status);
    }

    [Fact]
    public async Task Invoke_ScopeConventionInsteadOfAttribute_BehavesIdentically()
    {
        using var provider = BuildProvider();

        var builder = new TestConventionBuilder();

        builder.RequireScopesAll("billing.read");

        var status = await InvokeAsync(provider, ClaimsPrincipalOrNone.User(("scope", "billing.read")), [.. builder.BuildMetadata()]);

        Assert.Equal(Ok, status);
    }

    [Fact]
    public void Attribute_NoAccessesNamed_ThrowsAtConstruction()
    {
        // The predecessor treated "all of nothing" as satisfied, so an attribute naming no accesses
        // admitted everyone. It now cannot be constructed at all.
        Assert.Throws<ArgumentException>(() => new AuthorizeAllAccessAttribute());
        Assert.Throws<ArgumentException>(() => new AuthorizeAnyAccessAttribute());
        Assert.Throws<ArgumentException>(() => new AuthorizeAllScopesAttribute());
        Assert.Throws<ArgumentException>(() => new AuthorizeAnyScopesAttribute());
    }

    [Fact]
    public void Attribute_OnlyBlankAccessesNamed_ThrowsAtConstruction()
    {
        Assert.Throws<ArgumentException>(() => new AuthorizeAnyAccessAttribute("  ", string.Empty));
    }

    /// <summary>
    /// Collects the conventions an endpoint extension applies, so the metadata can be inspected
    /// without standing up a host.
    /// </summary>
    private sealed class TestConventionBuilder : IEndpointConventionBuilder
    {
        private readonly List<Action<EndpointBuilder>> conventions = [];

        public void Add(Action<EndpointBuilder> convention)
        {
            this.conventions.Add(convention);
        }

        public IList<object> BuildMetadata()
        {
            var endpointBuilder = new TestEndpointBuilder();

            foreach (var convention in this.conventions)
            {
                convention(endpointBuilder);
            }

            return endpointBuilder.Metadata;
        }
    }

    /// <summary>
    /// The minimum endpoint builder needed to collect metadata.
    /// </summary>
    private sealed class TestEndpointBuilder : EndpointBuilder
    {
        public override Endpoint Build()
        {
            return new Endpoint(this.RequestDelegate, new EndpointMetadataCollection(this.Metadata), this.DisplayName);
        }
    }
}

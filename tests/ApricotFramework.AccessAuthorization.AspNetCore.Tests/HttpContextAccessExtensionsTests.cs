using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

/// <summary>
/// The capability listing a user interface asks for, which must agree with the gates or it hides a
/// button that would have worked, or offers one that returns 403.
/// </summary>
public class HttpContextAccessExtensionsTests
{
    private const string Read = "sales:orders:read";

    private const string Edit = "sales:orders:edit";

    private const string Purge = "sales:orders:archive";

    private static ServiceProvider BuildProvider(bool withCatalog, params string[] assigned)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessStore(new CountingAccessStore(assigned));
        services.AddAccessRule<OwnerCanEditRule>();

        if (withCatalog)
        {
            services.AddAccessCatalog(
            [
                new AccessDefinition(Read, "sales:order"),
                new AccessDefinition(Edit, "sales:order"),
                new AccessDefinition(Purge, "sales:order"),
                new AccessDefinition("billing:invoices:read", "billing:invoice"),
            ]);
        }

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static DefaultHttpContext CreateContext(IServiceScope scope, params (string Type, string Value)[] claims)
    {
        return new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = ClaimsPrincipalOrNone.User(claims).Principal,
        };
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_WithResource_SkipsUnrelatedResourceTypes()
    {
        using var provider = BuildProvider(true, Read);
        using var scope = provider.CreateScope();

        var evaluation = await CreateContext(scope, ("sub", "u1")).GetAllowedAccessesAsync(AccessResource.Create("sales:order", "42"), Ct.Token);

        Assert.Equal([Read], evaluation.Allowed);
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_ResourceAttributeRule_WidensTheListing()
    {
        using var provider = BuildProvider(true, Read);
        using var scope = provider.CreateScope();

        var owned = AccessResource.Create("sales:order", "42", new Dictionary<string, object?> { ["ownerId"] = "u1" });

        var evaluation = await CreateContext(scope, ("sub", "u1")).GetAllowedAccessesAsync(owned, Ct.Token);

        Assert.Equal([Edit, Read], evaluation.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_ListingAndGate_Agree()
    {
        using var provider = BuildProvider(true, Read);
        using var scope = provider.CreateScope();

        var owned = AccessResource.Create("sales:order", "42", new Dictionary<string, object?> { ["ownerId"] = "u1" });
        var httpContext = CreateContext(scope, ("sub", "u1"));

        var evaluation = await httpContext.GetAllowedAccessesAsync(owned, Ct.Token);

        var authorization = scope.ServiceProvider.GetRequiredService<IAccessAuthorization>();
        var context = AccessContext.For(httpContext.GetAccessSubject()!, owned);

        foreach (var access in new[] { Read, Edit, Purge })
        {
            Assert.Equal(evaluation.Contains(access), await authorization.CheckAllAsync(context, [access], Ct.Token));
        }
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_NoResource_ConsidersEverythingDeclared()
    {
        using var provider = BuildProvider(true, Read, "billing:invoices:read");
        using var scope = provider.CreateScope();

        var evaluation = await CreateContext(scope, ("sub", "u1")).GetAllowedAccessesAsync(cancellationToken: Ct.Token);

        Assert.Equal(["billing:invoices:read", Read], evaluation.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_AnonymousCaller_AllowsNothing()
    {
        using var provider = BuildProvider(true, Read);
        using var scope = provider.CreateScope();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = ClaimsPrincipalOrNone.Anonymous().Principal,
        };

        Assert.Empty((await httpContext.GetAllowedAccessesAsync(cancellationToken: Ct.Token)).Allowed);
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_NoCatalogRegistered_Throws()
    {
        // Returning an empty set would hide every operation in the interface and look like a
        // permissions problem. A missing registration is a wiring mistake, so it says so.
        using var provider = BuildProvider(false, Read);
        using var scope = provider.CreateScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateContext(scope, ("sub", "u1")).GetAllowedAccessesAsync(cancellationToken: Ct.Token));

        Assert.Contains("AddAccessCatalog", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetAllowedAccessesAsync_SeveralCatalogs_AreAllConsidered()
    {
        // Two modules each declaring their own accesses is the normal case; one must not displace
        // the other.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessStore(new CountingAccessStore(Read, "billing:invoices:read"));
        services.AddAccessCatalog([new AccessDefinition(Read)]);
        services.AddAccessCatalog([new AccessDefinition("billing:invoices:read")]);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var evaluation = await CreateContext(scope, ("sub", "u1")).GetAllowedAccessesAsync(cancellationToken: Ct.Token);

        Assert.Equal(["billing:invoices:read", Read], evaluation.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetAccessSubject_AuthenticatedCaller_ReturnsTheSubject()
    {
        using var provider = BuildProvider(true);
        using var scope = provider.CreateScope();

        Assert.Equal("u1", CreateContext(scope, ("sub", "u1")).GetAccessSubject()!.Id);
    }

    [Fact]
    public void Extensions_NullContext_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => ((HttpContext)null!).GetAccessSubject());
    }

    /// <summary>
    /// A rule granting the edit access to whoever owns the instance.
    /// </summary>
    private sealed class OwnerCanEditRule : AccessRule
    {
        public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(access, Edit, StringComparison.Ordinal) || context.Resource is null)
            {
                return Task.FromResult(AccessDecision.Abstain);
            }

            var owner = context.Resource.Attributes.GetValueOrDefault("ownerId") as string;

            return Task.FromResult(string.Equals(owner, context.Subject.Id, StringComparison.Ordinal) ? AccessDecision.Allow : AccessDecision.Abstain);
        }
    }
}

using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

/// <summary>
/// Asking the catalog what the application declares is a first-class question, separate from asking
/// what a subject may do. There is exactly one catalog service and it is always the union.
/// </summary>
public class AccessCatalogRegistrationTests
{
    [Fact]
    public void GetRequiredService_SeveralCatalogs_ReturnsTheUnion()
    {
        // Registering each catalog under the interface would hand back whichever was last, silently
        // dropping the others.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessCatalog(["sales:orders:read"]);
        services.AddAccessCatalog([new AccessDefinition("billing:invoices:read", "billing:invoice")]);
        services.AddAccessCatalog<ExtraCatalog>();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var declared = provider.GetRequiredService<IAccessCatalog>().GetDefinitions().Select(definition => definition.Access);

        Assert.Equal(["billing:invoices:read", "extra:thing:do", "sales:orders:read"], declared.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetServices_Always_ExposesExactlyOneCatalog()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessCatalog(["a"]);
        services.AddAccessCatalog(["b"]);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Single(provider.GetServices<IAccessCatalog>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAccessCatalog_EitherSideOfAddAccessAuthorization_IsStillIncluded(bool catalogFirst)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        if (catalogFirst)
        {
            services.AddAccessCatalog(["a"]);
            services.AddAccessAuthorization();
        }
        else
        {
            services.AddAccessAuthorization();
            services.AddAccessCatalog(["a"]);
        }

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Single(provider.GetRequiredService<IAccessCatalog>().GetDefinitions());
    }

    [Fact]
    public void GetCandidates_ForAResourceType_NarrowsToWhatAppliesToIt()
    {
        // "What is declared about this kind of thing" — the question behind a capability listing.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();
        services.AddAccessCatalog(
        [
            new AccessDefinition("sales:orders:read", "sales:order"),
            new AccessDefinition("sales:orders:edit", "sales:order"),
            new AccessDefinition("billing:invoices:read", "billing:invoice"),
            new AccessDefinition("audit:read"),
        ]);

        using var provider = services.BuildServiceProvider(validateScopes: true);
        var catalog = provider.GetRequiredService<IAccessCatalog>();

        var forOrders = catalog.GetCandidates(AccessResource.OfType("sales:order"));

        Assert.Equal(["audit:read", "sales:orders:edit", "sales:orders:read"], forOrders.Order(StringComparer.Ordinal));
        Assert.Equal(4, catalog.GetDefinitions().Count);
    }

    [Fact]
    public void GetRequiredService_NoCatalogDeclared_IsEmptyRatherThanMissing()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Empty(provider.GetRequiredService<IAccessCatalog>().GetDefinitions());
    }

    /// <summary>
    /// A catalog resolved from the container rather than supplied as an instance.
    /// </summary>
    private sealed class ExtraCatalog : IAccessCatalog
    {
        public IReadOnlyList<AccessDefinition> GetDefinitions()
        {
            return [new AccessDefinition("extra:thing:do")];
        }
    }
}

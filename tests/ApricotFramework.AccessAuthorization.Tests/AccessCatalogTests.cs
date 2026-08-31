using ApricotFramework.AccessAuthorization.Extensions;
using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessCatalogTests
{
    [Fact]
    public void GetDefinitions_FromAccessStrings_AppliesToAnyResource()
    {
        var catalog = new StaticAccessCatalog(["a", "b"]);

        Assert.All(catalog.GetDefinitions(), definition => Assert.Null(definition.ResourceType));
    }

    [Fact]
    public void GetDefinitions_DuplicateDeclarations_AreCollapsed()
    {
        // Two components declaring the same access is legitimate, and enumerating it twice would
        // change nothing, so this is collapsed rather than rejected.
        var catalog = new StaticAccessCatalog(["a", "a", "b"]);

        Assert.Equal(2, catalog.GetDefinitions().Count);
    }

    [Fact]
    public void GetCandidates_NoResource_ReturnsEverythingDeclared()
    {
        var catalog = new StaticAccessCatalog([new AccessDefinition("a"), new AccessDefinition("b", "sales:order")]);

        Assert.Equal(["a", "b"], catalog.GetCandidates(null).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetCandidates_WithResource_SkipsOtherResourceTypes()
    {
        var catalog = new StaticAccessCatalog(
        [
            new AccessDefinition("any"),
            new AccessDefinition("order", "sales:order"),
            new AccessDefinition("payment", "billing:invoice"),
        ]);

        var candidates = catalog.GetCandidates(AccessResource.Create("sales:order", "42"));

        Assert.Equal(["any", "order"], candidates.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetCandidates_ResourceTypeDifferingInCase_IsNotAMatch()
    {
        var catalog = new StaticAccessCatalog([new AccessDefinition("order", "sales:order")]);

        Assert.Empty(catalog.GetCandidates(AccessResource.OfType("Content:Order")));
    }

    [Fact]
    public void Constructor_NullDefinitionAmongThem_ThrowsWhereItWasSupplied()
    {
        // Declared collections often come from assemblies with no nullable annotations, so the
        // signature guarantees nothing. Failing here names the registration; letting it through fails
        // later inside a capability listing with nothing to point at.
        var definitions = new List<AccessDefinition> { new("a"), null!, new("b") };

        var exception = Assert.Throws<ArgumentException>(() => new StaticAccessCatalog(definitions));

        Assert.Equal("definitions", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullDefinitionReachingTheComposite_ThrowsToo()
    {
        var exception = Assert.Throws<ArgumentException>(() => new CompositeAccessCatalog([new LeakyCatalog()]));

        Assert.Equal("catalogs", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullCatalogAmongThem_Throws()
    {
        Assert.Throws<ArgumentException>(() => new CompositeAccessCatalog([new StaticAccessCatalog(["a"]), null!]));
    }

    [Fact]
    public void Constructor_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new StaticAccessCatalog((IEnumerable<string>)null!));
        Assert.Throws<ArgumentNullException>(() => new StaticAccessCatalog((IEnumerable<AccessDefinition>)null!));
        Assert.Throws<ArgumentNullException>(() => new CompositeAccessCatalog(null!));
    }

    /// <summary>
    /// A catalog whose own definitions contain a null, which only the composite can discover.
    /// </summary>
    private sealed class LeakyCatalog : IAccessCatalog
    {
        public IReadOnlyList<AccessDefinition> GetDefinitions()
        {
            return [new AccessDefinition("a"), null!];
        }
    }
}

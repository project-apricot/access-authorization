namespace ApricotFramework.AccessAuthorization.Impl;

/// <summary>
/// One catalog over several, so separate modules can each declare their own accesses.
/// </summary>
public class CompositeAccessCatalog : IAccessCatalog
{
    /// <summary>
    /// The combined definitions, computed once.
    /// </summary>
    private readonly IReadOnlyList<AccessDefinition> definitions;

    /// <summary>
    /// Creates a new composite catalog.
    /// </summary>
    /// <param name="catalogs">The catalogs to combine.</param>
    public CompositeAccessCatalog(IEnumerable<IAccessCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);

        var combined = AccessDefinitions.WithoutNulls(catalogs, nameof(catalogs))
            .SelectMany(catalog => AccessDefinitions.WithoutNulls(catalog.GetDefinitions(), nameof(catalogs)));

        this.definitions = [.. combined.Distinct()];
    }

    /// <inheritdoc />
    public IReadOnlyList<AccessDefinition> GetDefinitions()
    {
        return this.definitions;
    }
}

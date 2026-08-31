namespace ApricotFramework.AccessAuthorization.Extensions;

/// <summary>
/// Turns a catalog into the candidate set for an enumeration.
/// </summary>
public static class AccessCatalogExtensions
{
    /// <summary>
    /// Gets the accesses worth asking about for a resource.
    /// </summary>
    /// <param name="catalog">The catalog.</param>
    /// <param name="resource">The resource, or <see langword="null"/> for none.</param>
    /// <returns>The candidate accesses.</returns>
    /// <remarks>
    /// With a resource, only definitions that apply to its type or to any type are candidates. With
    /// no resource, everything declared is.
    /// </remarks>
    public static IReadOnlySet<string> GetCandidates(this IAccessCatalog catalog, AccessResource? resource)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var definitions = catalog.GetDefinitions();

        var applicable = resource is null
            ? definitions
            : definitions.Where(definition => definition.ResourceType is null || string.Equals(definition.ResourceType, resource.Type, StringComparison.Ordinal)).ToList();

        return AccessSets.ToFrozenOrdinalSet(applicable.Select(definition => definition.Access));
    }
}

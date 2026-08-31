namespace ApricotFramework.AccessAuthorization.Impl;

/// <summary>
/// A catalog over a fixed list of definitions.
/// </summary>
public class StaticAccessCatalog : IAccessCatalog
{
    /// <summary>
    /// The declared definitions.
    /// </summary>
    private readonly IReadOnlyList<AccessDefinition> definitions;

    /// <summary>
    /// Creates a catalog from access strings that apply to any resource.
    /// </summary>
    /// <param name="accesses">The declared accesses.</param>
    public StaticAccessCatalog(IEnumerable<string> accesses)
        : this((accesses ?? throw new ArgumentNullException(nameof(accesses))).Select(access => new AccessDefinition(access)))
    {
    }

    /// <summary>
    /// Creates a catalog from definitions.
    /// </summary>
    /// <param name="definitions">The declared definitions.</param>
    public StaticAccessCatalog(IEnumerable<AccessDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        // Duplicates are tolerated rather than rejected: two components may legitimately declare the
        // same access, and enumerating it twice would change nothing. A null is not tolerated, because
        // it would survive to the first enumeration and fail there instead of here.
        this.definitions = [.. AccessDefinitions.WithoutNulls(definitions, nameof(definitions)).Distinct()];
    }

    /// <inheritdoc />
    public IReadOnlyList<AccessDefinition> GetDefinitions()
    {
        return this.definitions;
    }
}

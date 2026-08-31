namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// The accesses this application declares.
/// </summary>
/// <remarks>
/// Answering "what is this subject allowed to do?" needs a candidate set, because a rule is a
/// predicate and a predicate cannot be inverted. The catalog is that set.
/// </remarks>
public interface IAccessCatalog
{
    /// <summary>
    /// Gets the declared accesses.
    /// </summary>
    /// <returns>The declared accesses.</returns>
    IReadOnlyList<AccessDefinition> GetDefinitions();
}

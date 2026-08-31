namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// How many of a set of requirements must be satisfied.
/// </summary>
public enum RequirementMatch
{
    /// <summary>
    /// At least one requirement must be satisfied.
    /// </summary>
    Any = 0,

    /// <summary>
    /// Every requirement must be satisfied.
    /// </summary>
    All = 1,
}

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// What a rule has to say about one access.
/// </summary>
/// <remarks>
/// A rule that cannot reach the data it needs should let the exception propagate rather than
/// abstain: an outage is a different answer as "not applicable", and reporting it as one turns a
/// fault into a silent denial.
/// </remarks>
public enum AccessDecision
{
    /// <summary>
    /// The rule has no opinion, so the next rule decides. The default.
    /// </summary>
    Abstain = 0,

    /// <summary>
    /// The rule grants the access, and no later rule is consulted for it.
    /// </summary>
    Allow = 1,

    /// <summary>
    /// The rule refuses the access, and no later rule can grant it.
    /// </summary>
    Deny = 2
}

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// One step of the decision pipeline: given a context, what does this rule say about an access?
/// </summary>
/// <remarks>
/// Rules never see whether the caller wanted any or all of a set — that arithmetic happens after each
/// access has been decided alone, which is what lets one code path answer both "is this allowed" and
/// "what is allowed". Derive from <see cref="AccessRule"/> rather than implementing this directly.
/// </remarks>
public interface IAccessRule
{
    /// <summary>
    /// Decides one access.
    /// </summary>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="access">The access being asked about.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The decision.</returns>
    Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decides several accesses at once.
    /// </summary>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="accesses">The accesses no earlier rule has decided.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The accesses this rule grants and refuses; the rest are abstained on.</returns>
    /// <remarks>
    /// Override this when the rule can answer for many accesses in one round trip. It must agree
    /// with <see cref="EvaluateAsync"/> for every access.
    /// </remarks>
    Task<AccessRuleGrants> EvaluateManyAsync(AccessContext context, IReadOnlySet<string> accesses, CancellationToken cancellationToken = default);
}

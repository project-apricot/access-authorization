namespace ApricotFramework.AccessAuthorization.Impl;

/// <summary>
/// Grants whatever the subject has been statically assigned. Always present, and the reason the
/// ordinary role- or group-driven setup needs no rule of its own.
/// </summary>
/// <remarks>
/// The resource is ignored: an assigned <c>sales:orders:edit</c> means the subject may edit orders,
/// not that they may edit one particular order. Narrowing a grant to an instance is a rule of your
/// own, registered after this one.
/// </remarks>
public class AssignedAccessRule : AccessRule
{
    /// <summary>
    /// The resolver supplying the effective assigned set.
    /// </summary>
    private readonly IAccessResolver resolver;

    /// <summary>
    /// Creates a new rule.
    /// </summary>
    /// <param name="resolver">The resolver supplying the effective assigned set.</param>
    public AssignedAccessRule(IAccessResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        this.resolver = resolver;
    }

    /// <inheritdoc />
    public override async Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(access);

        var assigned = await this.resolver.ResolveAsync(context.Subject, cancellationToken).ConfigureAwait(false);

        return assigned.Contains(access) ? AccessDecision.Allow : AccessDecision.Abstain;
    }

    /// <inheritdoc />
    public override async Task<AccessRuleGrants> EvaluateManyAsync(AccessContext context, IReadOnlySet<string> accesses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(accesses);

        if (accesses.Count == 0)
        {
            return AccessRuleGrants.None;
        }

        // One resolve for the whole batch, which is what keeps a capability listing to a single
        // store round trip regardless of how many accesses the catalog declares.
        var assigned = await this.resolver.ResolveAsync(context.Subject, cancellationToken).ConfigureAwait(false);

        var allowed = AccessSets.ToOrdinalSet(accesses.Where(assigned.Contains));

        return allowed.Count == 0 ? AccessRuleGrants.None : new AccessRuleGrants(allowed, null);
    }
}

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Base class for rules, supplying the batched form as a loop over the single-access one.
/// </summary>
public abstract class AccessRule : IAccessRule
{
    /// <inheritdoc />
    public abstract Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public virtual async Task<AccessRuleGrants> EvaluateManyAsync(AccessContext context, IReadOnlySet<string> accesses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accesses);

        HashSet<string>? allowed = null;
        HashSet<string>? denied = null;

        foreach (var access in accesses)
        {
            var decision = await this.EvaluateAsync(context, access, cancellationToken).ConfigureAwait(false);

            switch (decision)
            {
                case AccessDecision.Allow:
                    allowed ??= new HashSet<string>(StringComparer.Ordinal);
                    allowed.Add(access);
                    break;

                case AccessDecision.Deny:
                    denied ??= new HashSet<string>(StringComparer.Ordinal);
                    denied.Add(access);
                    break;
            }
        }

        return allowed is null && denied is null ? AccessRuleGrants.None : new AccessRuleGrants(allowed, denied);
    }
}

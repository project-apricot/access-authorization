using System.Collections.Frozen;

namespace ApricotFramework.AccessAuthorization.Impl;

/// <summary>
/// Walks the rule pipeline once per request, letting the first rule with an opinion settle each
/// access and denying anything nothing spoke for.
/// </summary>
public class DefaultAccessAuthorization : IAccessAuthorization
{
    /// <summary>
    /// The pipeline, in the order rules are consulted.
    /// </summary>
    private readonly IReadOnlyList<IAccessRule> rules;

    /// <summary>
    /// Creates a new decision point.
    /// </summary>
    /// <param name="rules">The pipeline, in the order rules are to be consulted.</param>
    public DefaultAccessAuthorization(IEnumerable<IAccessRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        this.rules = [.. rules];
    }

    /// <inheritdoc />
    public virtual async Task<AccessEvaluation> GetAllowedAsync(AccessContext context, IReadOnlySet<string> candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return AccessEvaluation.Empty;
        }

        var undecided = AccessSets.ToOrdinalSet(candidates);
        var allowed = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in this.rules)
        {
            if (undecided.Count == 0)
            {
                break;
            }

            // A snapshot, so a rule, cannot reach the working set by casting or by holding a reference.
            var grants = await rule.EvaluateManyAsync(context, undecided.ToFrozenSet(StringComparer.Ordinal), cancellationToken).ConfigureAwait(false);

            // Denials are applied first, so a rule that names the same access in both sets fails closed.
            foreach (var denied in grants.Denied)
            {
                undecided.Remove(denied);
            }

            foreach (var granted in grants.Allowed)
            {
                if (undecided.Remove(granted))
                {
                    allowed.Add(granted);
                }
            }
        }

        return allowed.Count == 0 ? AccessEvaluation.Empty : new AccessEvaluation(allowed);
    }
}

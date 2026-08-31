using System.Collections.Frozen;

namespace ApricotFramework.AccessAuthorization.Extensions;

/// <summary>
/// The ordinary shapes of an authorization question, over the single primitive.
/// </summary>
public static class AccessAuthorizationExtensions
{
    /// <summary>
    /// Determines which of the candidate accesses are allowed.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="candidates">The accesses to decide.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed subset of the candidates.</returns>
    public static Task<AccessEvaluation> GetAllowedAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(candidates);

        return authorization.GetAllowedAsync(context, AccessSets.ToFrozenOrdinalSet(candidates), cancellationToken);
    }

    /// <summary>
    /// Determines everything the catalog declares that is allowed in a context.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="catalog">The catalog supplying the candidates.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed accesses.</returns>
    /// <remarks>
    /// This is the call a user interface makes to decide what to offer. Because it runs the same
    /// pipeline as the gate, an access it lists is one the gate will accept.
    /// </remarks>
    public static Task<AccessEvaluation> GetAllowedAsync(this IAccessAuthorization authorization, IAccessCatalog catalog, AccessContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(context);

        return authorization.GetAllowedAsync(context, catalog.GetCandidates(context.Resource), cancellationToken);
    }

    /// <summary>
    /// Checks whether any of the requirements is allowed.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to check. An empty set is never satisfied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when at least one requirement is allowed.</returns>
    public static async Task<bool> CheckAnyAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, CancellationToken cancellationToken = default)
    {
        var required = AccessSets.ToFrozenOrdinalSet(requirements);
        var allowed = await GetAllowedInternalAsync(authorization, context, required, cancellationToken).ConfigureAwait(false);

        return allowed.Allowed.Count > 0;
    }

    /// <summary>
    /// Checks whether all of the requirements are allowed.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to check. An empty set is never satisfied.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when every requirement is allowed.</returns>
    /// <remarks>
    /// An empty set denies rather than succeeding vacuously, so a requirement that lost its accesses
    /// closes the gate instead of opening it.
    /// </remarks>
    public static async Task<bool> CheckAllAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, CancellationToken cancellationToken = default)
    {
        var required = AccessSets.ToFrozenOrdinalSet(requirements);
        var allowed = await GetAllowedInternalAsync(authorization, context, required, cancellationToken).ConfigureAwait(false);

        return required.Count > 0 && required.Count == allowed.Allowed.Count;
    }

    /// <summary>
    /// Checks whether the requirements are satisfied under a match mode.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to check. An empty set is never satisfied.</param>
    /// <param name="match">How many requirements must be allowed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the requirements are satisfied.</returns>
    public static Task<bool> CheckAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, RequirementMatch match, CancellationToken cancellationToken = default)
    {
        return match == RequirementMatch.All
            ? authorization.CheckAllAsync(context, requirements, cancellationToken)
            : authorization.CheckAnyAsync(context, requirements, cancellationToken);
    }

    /// <summary>
    /// Requires any of the accesses, throwing when none is allowed.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to require. An empty set always throws.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the requirement is satisfied.</returns>
    /// <exception cref="AccessAuthorizationException">None of the accesses is allowed.</exception>
    public static async Task RequireAnyAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, CancellationToken cancellationToken = default)
    {
        var required = AccessSets.ToFrozenOrdinalSet(requirements);
        var allowed = await GetAllowedInternalAsync(authorization, context, required, cancellationToken).ConfigureAwait(false);

        if (allowed.Allowed.Count > 0)
        {
            return;
        }

        throw new AccessAuthorizationException(
            $"None of the required accesses is granted: {string.Join(", ", required)}.",
            required);
    }

    /// <summary>
    /// Requires all of the accesses, throwing when any is missing.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to require. An empty set always throws.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the requirement is satisfied.</returns>
    /// <exception cref="AccessAuthorizationException">At least one access is not allowed.</exception>
    public static async Task RequireAllAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, CancellationToken cancellationToken = default)
    {
        var required = AccessSets.ToFrozenOrdinalSet(requirements);
        var allowed = await GetAllowedInternalAsync(authorization, context, required, cancellationToken).ConfigureAwait(false);

        var missing = required.Where(access => !allowed.Allowed.Contains(access)).ToList();

        if (required.Count > 0 && missing.Count == 0)
        {
            return;
        }

        throw new AccessAuthorizationException(
            required.Count == 0
                ? "An access requirement listing no accesses can never be satisfied."
                : $"Some of the required accesses are not granted: {string.Join(", ", missing)}.",
            missing);
    }

    /// <summary>
    /// Requires the accesses under a match mode.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="requirements">The accesses to require. An empty set always throws.</param>
    /// <param name="match">How many requirements must be allowed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when the requirement is satisfied.</returns>
    /// <exception cref="AccessAuthorizationException">The requirement is not satisfied.</exception>
    public static Task RequireAsync(this IAccessAuthorization authorization, AccessContext context, IEnumerable<string> requirements, RequirementMatch match, CancellationToken cancellationToken = default)
    {
        return match == RequirementMatch.All
            ? authorization.RequireAllAsync(context, requirements, cancellationToken)
            : authorization.RequireAnyAsync(context, requirements, cancellationToken);
    }

    /// <summary>
    /// Runs the primitive, skipping it entirely when there is nothing to decide.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="required">The accesses to decide.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed subset.</returns>
    private static Task<AccessEvaluation> GetAllowedInternalAsync(IAccessAuthorization authorization, AccessContext context, FrozenSet<string> required, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(context);

        return required.Count == 0
            ? Task.FromResult(AccessEvaluation.Empty)
            : authorization.GetAllowedAsync(context, required, cancellationToken);
    }
}

using System.Security.Claims;

namespace ApricotFramework.AccessAuthorization.Scopes.Extensions;

/// <summary>
/// The ordinary shapes of a scope question.
/// </summary>
public static class ScopeAuthorizationExtensions
{
    /// <summary>
    /// Checks whether any of the scopes is present.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to check. An empty set is never satisfied.</param>
    /// <returns><see langword="true"/> when at least one scope is present.</returns>
    public static bool CheckAny(this IScopeAuthorization authorization, ClaimsPrincipal principal, IEnumerable<string> requirements)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return authorization.Check(principal, requirements, RequirementMatch.Any);
    }

    /// <summary>
    /// Checks whether all the scopes are present.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to check. An empty set is never satisfied.</param>
    /// <returns><see langword="true"/> when every scope is present.</returns>
    public static bool CheckAll(this IScopeAuthorization authorization, ClaimsPrincipal principal, IEnumerable<string> requirements)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return authorization.Check(principal, requirements, RequirementMatch.All);
    }

    /// <summary>
    /// Requires any of the scopes, throwing when none is present.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to require. An empty set always throws.</param>
    /// <exception cref="ScopeAuthorizationException">None of the scopes is present.</exception>
    public static void RequireAny(this IScopeAuthorization authorization, ClaimsPrincipal principal, IEnumerable<string> requirements)
    {
        Require(authorization, principal, requirements, RequirementMatch.Any);
    }

    /// <summary>
    /// Requires all of the scopes, throwing when any is absent.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to require. An empty set always throws.</param>
    /// <exception cref="ScopeAuthorizationException">At least one scope is absent.</exception>
    public static void RequireAll(this IScopeAuthorization authorization, ClaimsPrincipal principal, IEnumerable<string> requirements)
    {
        Require(authorization, principal, requirements, RequirementMatch.All);
    }

    /// <summary>
    /// Requires the scopes under a match mode, throwing when unsatisfied.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to require. An empty set always throws.</param>
    /// <param name="match">How many requirements must be present.</param>
    /// <exception cref="ScopeAuthorizationException">The requirement is not satisfied.</exception>
    public static void Require(this IScopeAuthorization authorization, ClaimsPrincipal principal, IEnumerable<string> requirements, RequirementMatch match)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(requirements);

        var requirementsList = requirements.ToList();
        if (authorization.Check(principal, requirementsList, match))
        {
            return;
        }

        var required = requirementsList;

        throw new ScopeAuthorizationException(
            match == RequirementMatch.All
                ? $"Some of the required scopes are not granted: {string.Join(", ", required)}."
                : $"None of the required scopes is granted: {string.Join(", ", required)}.",
            required);
    }
}

using System.Security.Claims;
using ApricotFramework.AccessAuthorization.Scopes.Options;

namespace ApricotFramework.AccessAuthorization.Scopes.Impl;

/// <summary>
/// Reads scopes from a principal's claims and compares them ordinally, leaving where the settings
/// come from to the derived type.
/// </summary>
public abstract class ScopeAuthorizationBase : IScopeAuthorization
{
    /// <inheritdoc />
    public virtual bool Check(ClaimsPrincipal principal, IEnumerable<string> requirements, RequirementMatch match)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(requirements);

        var required = AccessSets.ToOrdinalSet(requirements);

        // An empty requirement is never satisfied, so a guard that lost its scopes closes rather
        // than opening.
        if (required.Count == 0)
        {
            return false;
        }

        var granted = this.GetScopes(principal);

        return match == RequirementMatch.All
            ? required.All(granted.Contains)
            : required.Any(granted.Contains);
    }

    /// <summary>
    /// Gets the settings in force for this call.
    /// </summary>
    /// <returns>The settings.</returns>
    protected abstract ScopeAuthorizationOptions GetCurrentOptions();

    /// <summary>
    /// Reads the granted scopes from a principal.
    /// </summary>
    /// <param name="principal">The principal.</param>
    /// <returns>The granted scopes.</returns>
    protected virtual IReadOnlySet<string> GetScopes(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var current = this.GetCurrentOptions();
        var claimTypes = AccessSets.ToOrdinalSet(current.ClaimTypes);
        var separators = (string.IsNullOrEmpty(current.Separators) ? " " : current.Separators).ToCharArray();

        var granted = new HashSet<string>(StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!claimTypes.Contains(claim.Type))
            {
                continue;
            }

            granted.UnionWith(claim.Value.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        return granted;
    }
}

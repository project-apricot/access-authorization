using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Requirements;

/// <summary>
/// An authorization requirement naming the token scopes an endpoint needs.
/// </summary>
public sealed class ScopeRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Creates a new requirement.
    /// </summary>
    /// <param name="scopes">The scopes required. Cannot be empty.</param>
    /// <param name="match">How many of them must be present.</param>
    /// <exception cref="ArgumentException">No scopes were named.</exception>
    public ScopeRequirement(IEnumerable<string> scopes, RequirementMatch match)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        var required = scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).ToHashSet(StringComparer.Ordinal);

        if (required.Count == 0)
        {
            throw new ArgumentException("A scope requirement must name at least one scope.", nameof(scopes));
        }

        this.Scopes = required;
        this.Match = match;
    }

    /// <summary>
    /// Gets the scopes required.
    /// </summary>
    public IReadOnlySet<string> Scopes { get; }

    /// <summary>
    /// Gets how many of the scopes must be present.
    /// </summary>
    public RequirementMatch Match { get; }
}

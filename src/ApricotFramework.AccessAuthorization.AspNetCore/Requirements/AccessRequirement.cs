using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Requirements;

/// <summary>
/// An authorization requirement naming the accesses an endpoint needs.
/// </summary>
public sealed class AccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Creates a new requirement.
    /// </summary>
    /// <param name="accesses">The accesses required. Cannot be empty.</param>
    /// <param name="match">How many of them must be granted.</param>
    /// <exception cref="ArgumentException">No accesses were named.</exception>
    public AccessRequirement(IEnumerable<string> accesses, RequirementMatch match)
    {
        ArgumentNullException.ThrowIfNull(accesses);

        var required = accesses.Where(access => !string.IsNullOrWhiteSpace(access)).ToHashSet(StringComparer.Ordinal);

        // Rejected at construction: a requirement naming nothing can never be satisfied, so failing
        // at startup beats every request returning 403 for a reason nobody can see.
        if (required.Count == 0)
        {
            throw new ArgumentException("An access requirement must name at least one access.", nameof(accesses));
        }

        this.Accesses = required;
        this.Match = match;
    }

    /// <summary>
    /// Gets the accesses required.
    /// </summary>
    public IReadOnlySet<string> Accesses { get; }

    /// <summary>
    /// Gets how many of the accesses must be granted.
    /// </summary>
    public RequirementMatch Match { get; }
}

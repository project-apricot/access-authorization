using System.Security.Claims;

namespace ApricotFramework.AccessAuthorization.Scopes;

/// <summary>
/// Checks the scopes a token was issued with. Independent of access authorization, and usable on its
/// own.
/// </summary>
/// <remarks>
/// Scopes say what a client was allowed to ask for; accesses say what a subject may do. Merging them
/// would let a scope satisfy an access requirement. A machine token with no subject can still be
/// authorized here, which is the usual service-to-service case.
/// </remarks>
public interface IScopeAuthorization
{
    /// <summary>
    /// Checks whether a principal's scopes satisfy the requirements.
    /// </summary>
    /// <param name="principal">The principal whose token scopes are read.</param>
    /// <param name="requirements">The scopes to check. An empty set is never satisfied.</param>
    /// <param name="match">How many requirements must be present.</param>
    /// <returns><see langword="true"/> when the requirements are satisfied.</returns>
    bool Check(ClaimsPrincipal principal, IEnumerable<string> requirements, RequirementMatch match);
}

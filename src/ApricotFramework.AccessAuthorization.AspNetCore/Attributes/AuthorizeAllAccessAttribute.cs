using ApricotFramework.AccessAuthorization.AspNetCore.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Attributes;

/// <summary>
/// Requires every one of the named accesses.
/// </summary>
/// <remarks>
/// Carries its requirement as endpoint metadata rather than running as an MVC filter, so the same
/// attribute governs a controller action, a minimal-API endpoint, and a gRPC method, and
/// <c>[AllowAnonymous]</c> is honored by the authorization middleware rather than by this library.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeAllAccessAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>
    /// The requirement for this attribute contributes.
    /// </summary>
    private readonly AccessRequirement requirement;

    /// <summary>
    /// Creates a new attribute.
    /// </summary>
    /// <param name="accesses">The accesses, all of which must be granted.</param>
    public AuthorizeAllAccessAttribute(params string[] accesses)
    {
        this.requirement = new AccessRequirement(accesses, RequirementMatch.All);
    }

    /// <summary>
    /// Gets the accesses, all of which must be granted.
    /// </summary>
    public IReadOnlySet<string> Accesses => this.requirement.Accesses;

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        return [this.requirement];
    }
}

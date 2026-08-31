using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Requirements;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Attributes;

/// <summary>
/// Requires at least one of the named token scopes.
/// </summary>
/// <remarks>
/// Works on a gRPC method as well as an MVC action, which is what replaces calling a scope check by
/// hand inside a service implementation.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AuthorizeAnyScopesAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>
    /// The requirement for this attribute contributes.
    /// </summary>
    private readonly ScopeRequirement requirement;

    /// <summary>
    /// Creates a new attribute.
    /// </summary>
    /// <param name="scopes">The scopes, at least one of which must be present.</param>
    public AuthorizeAnyScopesAttribute(params string[] scopes)
    {
        this.requirement = new ScopeRequirement(scopes, RequirementMatch.Any);
    }

    /// <summary>
    /// Gets the scopes, at least one of which must be present.
    /// </summary>
    public IReadOnlySet<string> Scopes => this.requirement.Scopes;

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        return [this.requirement];
    }
}

using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Requirements;
using ApricotFramework.AccessAuthorization.Scopes;
using Microsoft.AspNetCore.Authorization;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Handlers;

/// <summary>
/// Decides a <see cref="ScopeRequirement"/> from the principal's token scopes.
/// </summary>
public class ScopeAuthorizationHandler : AuthorizationHandler<ScopeRequirement>
{
    /// <summary>
    /// The scope authorization service.
    /// </summary>
    private readonly IScopeAuthorization authorization;

    /// <summary>
    /// Creates a new handler.
    /// </summary>
    /// <param name="authorization">The scope authorization service.</param>
    public ScopeAuthorizationHandler(IScopeAuthorization authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        this.authorization = authorization;
    }

    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (this.authorization.Check(context.User, requirement.Scopes, requirement.Match))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

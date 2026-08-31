using ApricotFramework.AccessAuthorization.AspNetCore.Requirements;
using ApricotFramework.AccessAuthorization.AspNetCore.Subjects;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Handlers;

/// <summary>
/// Decides an <see cref="AccessRequirement"/> through the rule pipeline.
/// </summary>
public class AccessAuthorizationHandler : AuthorizationHandler<AccessRequirement>
{
    /// <summary>
    /// The decision point.
    /// </summary>
    private readonly IAccessAuthorization authorization;

    /// <summary>
    /// The resolver turning the principal into a subject.
    /// </summary>
    private readonly IAccessSubjectResolver subjectResolver;

    /// <summary>
    /// Creates a new handler.
    /// </summary>
    /// <param name="authorization">The decision point.</param>
    /// <param name="subjectResolver">The resolver turning the principal into a subject.</param>
    public AccessAuthorizationHandler(IAccessAuthorization authorization, IAccessSubjectResolver subjectResolver)
    {
        ArgumentNullException.ThrowIfNull(authorization);
        ArgumentNullException.ThrowIfNull(subjectResolver);

        this.authorization = authorization;
        this.subjectResolver = subjectResolver;
    }

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var subject = this.subjectResolver.Resolve(context.User);

        // Not succeeding is the denial; there is nothing to fail explicitly, because another handler
        // may still satisfy the same requirement.
        if (subject is null)
        {
            return;
        }

        var cancellationToken = (context.Resource as HttpContext)?.RequestAborted ?? CancellationToken.None;

        var allowed = await this.authorization.CheckAsync(
            AccessContext.For(subject),
            requirement.Accesses,
            requirement.Match,
            cancellationToken).ConfigureAwait(false);

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}

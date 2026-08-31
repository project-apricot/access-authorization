using ApricotFramework.AccessAuthorization.AspNetCore.Requirements;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Requirements;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions;

/// <summary>
/// Gives a policy denial the same problem-details body an imperative one gets.
/// </summary>
/// <remarks>
/// A denial from an attribute raises no exception, so the exception mapper never sees it and the
/// middleware answers a bodyless 403. This fills that in, and only for a requirement this library
/// owns — anything else is left to whoever else wants to describe it.
/// </remarks>
internal sealed class AccessAuthorizationResultWriter : IAuthorizationMiddlewareResultHandler
{
    /// <summary>
    /// The payload key listing what the endpoint asked for.
    /// </summary>
    private const string RequiredPayloadKey = "required";

    /// <summary>
    /// The payload key saying whether any or all of them were needed.
    /// </summary>
    private const string MatchPayloadKey = "match";

    /// <summary>
    /// The handler being decorated.
    /// </summary>
    private readonly IAuthorizationMiddlewareResultHandler inner;

    /// <summary>
    /// Creates a new writer.
    /// </summary>
    /// <param name="inner">The handler to decorate.</param>
    public AccessAuthorizationResultWriter(IAuthorizationMiddlewareResultHandler inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        this.inner = inner;
    }

    /// <inheritdoc />
    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        // The inner handler picks the status code and any authenticate-scheme headers first.
        await this.inner.HandleAsync(next, context, policy, authorizeResult).ConfigureAwait(false);

        if (authorizeResult.Succeeded || context.Response.HasStarted)
        {
            return;
        }

        // Someone already described this failure. Writing again would corrupt the response, so the
        // decorators compose rather than fight, whichever order they were registered in.
        if (context.Response.ContentLength is > 0 || context.Response.ContentType is not null)
        {
            return;
        }

        var errors = Describe(context.Response.StatusCode, authorizeResult.AuthorizationFailure);

        if (errors is null)
        {
            return;
        }

        await context.WriteErrorProblemDetailsAsync(errors, context.Response.StatusCode, context.RequestAborted).ConfigureAwait(false);
    }

    /// <summary>
    /// Describes a failure caused by one of this library's requirements.
    /// </summary>
    /// <param name="statusCode">The status code the inner handler chose.</param>
    /// <param name="failure">The failure, when there was one.</param>
    /// <returns>The errors or null when this library did not cause the failure.</returns>
    private static IReadOnlyList<ErrorDefinition>? Describe(int statusCode, AuthorizationFailure? failure)
    {
        // Only 403. A 401 means the caller never said who it was, which is not this library's failure
        // to describe even when one of its requirements is also unmet.
        if (statusCode != StatusCodes.Status403Forbidden || failure is null)
        {
            return null;
        }

        foreach (var requirement in failure.FailedRequirements)
        {
            switch (requirement)
            {
                case AccessRequirement access:
                    return [Err.AccessDenied(
                        AccessAuthorizationErrors.AccessNotGranted,
                        "The required access is not granted.",
                        Payload(access.Accesses, access.Match))];

                case ScopeRequirement scope:
                    return [Err.AccessDenied(
                        AccessAuthorizationErrors.ScopeNotGranted,
                        "The required scope is not granted.",
                        Payload(scope.Scopes, scope.Match))];

                default:
                    continue;
            }
        }

        return null;
    }

    /// <summary>
    /// Builds the payload naming what the endpoint asked for.
    /// </summary>
    /// <param name="required">The accesses or scopes required.</param>
    /// <param name="match">How many of them were needed.</param>
    /// <returns>The payload.</returns>
    /// <remarks>
    /// This says what was <em>required</em>, not what was missing: a policy failure does not record
    /// which of the set went unmet, unlike the exception an imperative check throws.
    /// </remarks>
    private static Dictionary<string, object?> Payload(IReadOnlySet<string> required, RequirementMatch match)
    {
        return new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [RequiredPayloadKey] = required.Order(StringComparer.Ordinal).ToArray(),
            [MatchPayloadKey] = match == RequirementMatch.All ? "all" : "any",
        };
    }
}

using ApricotFramework.AccessAuthorization.Scopes;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions;

/// <summary>
/// Maps the authorization exceptions onto access-denied errors, naming what was missing.
/// </summary>
internal sealed class AuthorizationExceptionMapper : IExceptionErrorMapper
{
    /// <summary>
    /// The payload key listing what the caller lacked.
    /// </summary>
    private const string MissingPayloadKey = "missing";

    /// <inheritdoc />
    public IReadOnlyList<ErrorDefinition>? Map(HttpContext httpContext, Exception exception)
    {
        return exception switch
        {
            AccessAuthorizationException access =>
                [Err.AccessDenied(AccessAuthorizationErrors.AccessNotGranted, access.Message, Payload(access.MissingAccesses))],
            ScopeAuthorizationException scope =>
                [Err.AccessDenied(AccessAuthorizationErrors.ScopeNotGranted, scope.Message, Payload(scope.MissingScopes))],

            // Anything else, including the AuthorizationException base, is left to another mapper:
            // answering for an exception this library does not own would silence every mapper after it.
            _ => null,
        };
    }

    /// <summary>
    /// Builds the payload naming what was missing.
    /// </summary>
    /// <param name="missing">The accesses or scopes that were not granted.</param>
    /// <returns>The payload, or null when nothing was named.</returns>
    private static Dictionary<string, object?>? Payload(IReadOnlyList<string> missing)
    {
        return missing.Count == 0 ? null : new Dictionary<string, object?>(StringComparer.Ordinal) { [MissingPayloadKey] = missing };
    }
}

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions;

/// <summary>
/// The error codes this library reports.
/// </summary>
/// <remarks>
/// Distinct from the generic <c>ACCESS_DENIED</c>, so a client can tell missing access from a
/// missing token scope and say something useful about either.
/// </remarks>
public static class AccessAuthorizationErrors
{
    /// <summary>
    /// The subject lacks the access the operation requires.
    /// </summary>
    public const string AccessNotGranted = "ACCESS_NOT_GRANTED";

    /// <summary>
    /// The caller's token lacks a scope the operation requires.
    /// </summary>
    public const string ScopeNotGranted = "SCOPE_NOT_GRANTED";
}

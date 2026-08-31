using ApricotFramework.AccessAuthorization.Scopes.Options;

namespace ApricotFramework.AccessAuthorization.Options;

/// <summary>
/// Settings for authorization, bound from one configuration section.
/// </summary>
public sealed class AccessAuthorizationOptions
{
    /// <summary>
    /// Gets or sets how long a subject's effective assigned accesses may be reused.
    /// </summary>
    /// <remarks>
    /// Null or non-positive disables caching, which is the default: a cached grant delays a revocation
    /// by up to this long. Only the assigned set is ever cached, never a decision — a decision depends
    /// on attributes whose full extent is unknowable to a cache key.
    /// </remarks>
    public TimeSpan? CacheLifetime { get; set; }

    /// <summary>
    /// Gets how a subject is read out of an authenticated principal.
    /// </summary>
    public AccessSubjectOptions Subject { get; } = new AccessSubjectOptions();

    /// <summary>
    /// Gets how token scopes are read out of a principal.
    /// </summary>
    public ScopeAuthorizationOptions Scope { get; } = new ScopeAuthorizationOptions();
}

using ApricotFramework.AccessAuthorization.Scopes.Options;

namespace ApricotFramework.AccessAuthorization.Scopes.Impl;

/// <summary>
/// Scope authorization over settings fixed at construction, for a host with no options system.
/// </summary>
public class DefaultScopeAuthorization : ScopeAuthorizationBase
{
    /// <summary>
    /// The settings.
    /// </summary>
    private readonly ScopeAuthorizationOptions options;

    /// <summary>
    /// Creates a new instance using default settings.
    /// </summary>
    public DefaultScopeAuthorization() : this(null)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="options">The settings, or <see langword="null"/> for the defaults.</param>
    public DefaultScopeAuthorization(ScopeAuthorizationOptions? options)
    {
        this.options = options ?? new ScopeAuthorizationOptions();
    }

    /// <inheritdoc />
    protected override ScopeAuthorizationOptions GetCurrentOptions()
    {
        return this.options;
    }
}

using ApricotFramework.AccessAuthorization.Options;
using ApricotFramework.AccessAuthorization.Scopes.Impl;
using ApricotFramework.AccessAuthorization.Scopes.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Impl;

/// <summary>
/// Scope authorization reading its settings per call, so a configuration reload takes effect without
/// a restart.
/// </summary>
public sealed class OptionsAwareScopeAuthorization : ScopeAuthorizationBase
{
    /// <summary>
    /// The settings.
    /// </summary>
    private readonly IOptionsMonitor<AccessAuthorizationOptions> options;

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="options">The settings.</param>
    public OptionsAwareScopeAuthorization(IOptionsMonitor<AccessAuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    /// <inheritdoc />
    protected override ScopeAuthorizationOptions GetCurrentOptions()
    {
        return this.options.CurrentValue.Scope;
    }
}

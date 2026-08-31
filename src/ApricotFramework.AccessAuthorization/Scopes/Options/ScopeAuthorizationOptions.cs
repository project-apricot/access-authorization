namespace ApricotFramework.AccessAuthorization.Scopes.Options;

/// <summary>
/// Settings for scope authorization, bound from the <c>Scope</c> subsection.
/// </summary>
public sealed class ScopeAuthorizationOptions
{
    /// <summary>
    /// Gets the claim types scopes are read from, in no particular order; every match contributes.
    /// </summary>
    /// <remarks>
    /// Both spellings are read by default. RFC 9068 and IdentityServer issue <c>scope</c>; Microsoft
    /// Entra issues <c>scp</c>, and a deployment that read only the former would reject every Entra
    /// token.
    /// </remarks>
    public IList<string> ClaimTypes { get; } = ["scope", "scp"];

    /// <summary>
    /// Gets or sets the characters separating scopes within one claim value.
    /// </summary>
    public string Separators { get; set; } = " ";
}

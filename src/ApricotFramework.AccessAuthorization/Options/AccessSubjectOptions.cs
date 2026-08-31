using System.Security.Claims;

namespace ApricotFramework.AccessAuthorization.Options;

/// <summary>
/// How a subject is read out of an authenticated principal.
/// </summary>
public sealed class AccessSubjectOptions
{
    /// <summary>
    /// Gets the claim types the subject id is taken from, in order; the first with a value wins.
    /// </summary>
    /// <remarks>
    /// Both defaults are needed: JWT bearer authentication rewrites <c>sub</c> to
    /// <see cref="ClaimTypes.NameIdentifier"/> unless inbound claim mapping is turned off, so a
    /// deployment that read only one of them would work on exactly one configuration.
    /// </remarks>
    public IList<string> IdClaimTypes { get; } = ["sub", ClaimTypes.NameIdentifier];

    /// <summary>
    /// Gets the claims lifted onto the subject as attributes, keyed by claim type and valued by
    /// attribute name.
    /// </summary>
    /// <remarks>
    /// This is what makes an identity composite without the library knowing what the qualifier
    /// means — mapping <c>org_id</c> to <c>organization</c> produces a user acting in an
    /// organisation, which a rule can then read.
    /// </remarks>
    public IDictionary<string, string> AttributeClaims { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.Examples.Web.Auth;

/// <summary>
/// Builds a principal from request headers, so the example needs no token issuer and demonstrates
/// authorization rather than authentication.
/// </summary>
public sealed class DemoAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>
    /// The scheme name.
    /// </summary>
    public const string SchemeName = "Demo";

    /// <summary>
    /// Creates a new handler.
    /// </summary>
    /// <param name="options">The scheme options.</param>
    /// <param name="logger">The logger factory.</param>
    /// <param name="encoder">The URL encoder.</param>
    public DemoAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <inheritdoc />
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var subject = this.Request.Headers["X-Demo-Subject"].ToString();
        var organization = this.Request.Headers["X-Demo-Org"].ToString();
        var scopes = this.Request.Headers["X-Demo-Scopes"].ToString();

        // Nothing supplied at all means an anonymous caller, which is how the challenge path is
        // demonstrated.
        if (string.IsNullOrWhiteSpace(subject) && string.IsNullOrWhiteSpace(scopes))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();

        if (!string.IsNullOrWhiteSpace(subject))
        {
            claims.Add(new Claim("sub", subject));
        }

        if (!string.IsNullOrWhiteSpace(organization))
        {
            claims.Add(new Claim("org_id", organization));
        }

        if (!string.IsNullOrWhiteSpace(scopes))
        {
            claims.Add(new Claim("scope", scopes));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

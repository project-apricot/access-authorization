using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

/// <summary>
/// The ambient test cancellation token, so async calls stay responsive to a cancelled run.
/// </summary>
internal static class Ct
{
    internal static CancellationToken Token => TestContext.Current.CancellationToken;
}

/// <summary>
/// A caller, authenticated with claims or not authenticated at all.
/// </summary>
internal sealed class ClaimsPrincipalOrNone
{
    private ClaimsPrincipalOrNone(ClaimsPrincipal principal)
    {
        this.Principal = principal;
    }

    internal ClaimsPrincipal Principal { get; }

    internal static ClaimsPrincipalOrNone User(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipalOrNone(new ClaimsPrincipal(new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)), "Test")));
    }

    internal static ClaimsPrincipalOrNone Anonymous()
    {
        return new ClaimsPrincipalOrNone(new ClaimsPrincipal(new ClaimsIdentity()));
    }
}

/// <summary>
/// A store returning a fixed set and counting how often it was asked.
/// </summary>
internal sealed class CountingAccessStore : IAccessStore
{
    private readonly IReadOnlySet<string> accesses;

    public CountingAccessStore(params string[] accesses)
    {
        this.accesses = new HashSet<string>(accesses, StringComparer.Ordinal);
    }

    public int Calls { get; private set; }

    public Task<IReadOnlySet<string>> GetAccessesAsync(AccessSubject subject, CancellationToken cancellationToken = default)
    {
        this.Calls++;

        return Task.FromResult(this.accesses);
    }
}

/// <summary>
/// A rule granting every access it is asked about, standing in for an operator bypass.
/// </summary>
internal sealed class AllowAllRule : AccessRule
{
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessDecision.Allow);
    }
}

/// <summary>
/// A rule refusing every access it is asked about.
/// </summary>
internal sealed class DenyAllRule : AccessRule
{
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessDecision.Deny);
    }
}

/// <summary>
/// A decision point that records what it was asked and answers from a fixed set, standing in for a
/// remote one.
/// </summary>
internal sealed class StubAccessAuthorization : IAccessAuthorization
{
    private readonly IReadOnlySet<string> allowed;

    public StubAccessAuthorization(params string[] allowed)
    {
        this.allowed = new HashSet<string>(allowed, StringComparer.Ordinal);
    }

    public int Calls { get; private set; }

    public Task<AccessEvaluation> GetAllowedAsync(AccessContext context, IReadOnlySet<string> candidates, CancellationToken cancellationToken = default)
    {
        this.Calls++;

        return Task.FromResult(new AccessEvaluation(candidates.Where(this.allowed.Contains)));
    }
}

/// <summary>
/// An authentication scheme that never authenticates, so the middleware has somewhere to send a
/// challenge or a forbid and the resulting status code can be observed.
/// </summary>
internal sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    internal const string SchemeName = "Test";

    public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No result, so the policy evaluator falls back to the principal the test set directly.
        return Task.FromResult(AuthenticateResult.NoResult());
    }
}

using System.Security.Claims;
using ApricotFramework.AccessAuthorization.Scopes;
using ApricotFramework.AccessAuthorization.Scopes.Extensions;
using ApricotFramework.AccessAuthorization.Scopes.Impl;
using ApricotFramework.AccessAuthorization.Scopes.Options;

namespace ApricotFramework.AccessAuthorization.Tests;

public class ScopeAuthorizationTests
{
    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value))));
    }

    [Fact]
    public void Check_SpaceSeparatedScopeClaim_ReadsEveryScope()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.True(authorization.CheckAll(Principal(("scope", "billing.read billing.write")), ["billing.read", "billing.write"]));
    }

    [Fact]
    public void Check_ScpClaim_IsReadAsWell()
    {
        // Microsoft Entra issues scp rather than scope. Reading only the latter rejected every
        // token such an issuer produced.
        var authorization = new DefaultScopeAuthorization();

        Assert.True(authorization.CheckAll(Principal(("scp", "billing.read")), ["billing.read"]));
    }

    [Fact]
    public void Check_RepeatedScopeClaims_AreCombined()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.True(authorization.CheckAll(Principal(("scope", "billing.read"), ("scope", "billing.write")), ["billing.read", "billing.write"]));
    }

    [Fact]
    public void Check_AnyWithOneGranted_IsTrue()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.True(authorization.CheckAny(Principal(("scope", "billing.read")), ["billing.read", "billing.write"]));
    }

    [Fact]
    public void Check_AllWithOneMissing_IsFalse()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.False(authorization.CheckAll(Principal(("scope", "billing.read")), ["billing.read", "billing.write"]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Check_ScopeClaimWithNothingInIt_IsFalse(string value)
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.False(authorization.CheckAny(Principal(("scope", value)), ["billing.read"]));
    }

    [Fact]
    public void Check_NoRequirements_IsFalse()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.False(authorization.CheckAll(Principal(("scope", "billing.read")), []));
    }

    [Fact]
    public void Check_ScopeDifferingInCase_IsFalse()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.False(authorization.CheckAll(Principal(("scope", "Pay.Read")), ["billing.read"]));
    }

    [Fact]
    public void Check_UnauthenticatedPrincipal_IsFalse()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.False(authorization.CheckAny(new ClaimsPrincipal(new ClaimsIdentity()), ["billing.read"]));
    }

    [Fact]
    public void Check_CustomClaimTypeAndSeparator_AreHonoured()
    {
        var options = new ScopeAuthorizationOptions { Separators = " ," };
        options.ClaimTypes.Clear();
        options.ClaimTypes.Add("permissions");

        var authorization = new DefaultScopeAuthorization(options);

        Assert.True(authorization.CheckAll(Principal(("permissions", "billing.read,billing.write")), ["billing.read", "billing.write"]));
        Assert.False(authorization.CheckAny(Principal(("scope", "billing.read")), ["billing.read"]));
    }

    [Fact]
    public void RequireAll_OneMissing_ThrowsNamingTheRequirement()
    {
        var authorization = new DefaultScopeAuthorization();

        var exception = Assert.Throws<ScopeAuthorizationException>(() => authorization.RequireAll(Principal(("scope", "billing.read")), ["billing.read", "billing.write"]));

        Assert.Contains("billing.write", exception.MissingScopes);
    }

    [Fact]
    public void RequireAny_Satisfied_DoesNotThrow()
    {
        var authorization = new DefaultScopeAuthorization();

        authorization.RequireAny(Principal(("scope", "billing.read")), ["billing.read"]);
    }

    [Fact]
    public void Check_DerivedFromTheBase_SuppliesItsOwnSettings()
    {
        // The point of the split: a host with its own settings source derives from the base and
        // supplies only that, with no unused options instance to construct.
        var authorization = new SwitchableScopeAuthorization { ClaimType = "permissions" };

        Assert.True(authorization.CheckAll(Principal(("permissions", "billing.read")), ["billing.read"]));

        authorization.ClaimType = "scope";

        Assert.False(authorization.CheckAll(Principal(("permissions", "billing.read")), ["billing.read"]));
    }

    [Fact]
    public void Check_NullPrincipal_Throws()
    {
        var authorization = new DefaultScopeAuthorization();

        Assert.Throws<ArgumentNullException>(() => authorization.CheckAny(null!, ["billing.read"]));
    }

    /// <summary>
    /// Reads its settings from mutable state rather than from a fixed instance, standing in for a
    /// host with its own configuration source.
    /// </summary>
    private sealed class SwitchableScopeAuthorization : ScopeAuthorizationBase
    {
        public string ClaimType { get; set; } = "scope";

        protected override ScopeAuthorizationOptions GetCurrentOptions()
        {
            var options = new ScopeAuthorizationOptions();

            options.ClaimTypes.Clear();
            options.ClaimTypes.Add(this.ClaimType);

            return options;
        }
    }
}

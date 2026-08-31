using System.Security.Claims;
using ApricotFramework.AccessAuthorization.AspNetCore.Subjects;
using ApricotFramework.AccessAuthorization.Options;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

public class ClaimsAccessSubjectResolverTests
{
    private static ClaimsAccessSubjectResolver CreateResolver(Action<AccessAuthorizationOptions>? configure = null)
    {
        var options = new AccessAuthorizationOptions();

        configure?.Invoke(options);

        return new ClaimsAccessSubjectResolver(new StaticOptionsMonitor(options));
    }

    [Fact]
    public void Resolve_SubClaim_IsUsed()
    {
        var subject = CreateResolver().Resolve(ClaimsPrincipalOrNone.User(("sub", "u1")).Principal);

        Assert.Equal("u1", subject!.Id);
    }

    [Fact]
    public void Resolve_NameIdentifierOnly_IsUsedAsWell()
    {
        // JWT bearer authentication rewrites sub to NameIdentifier unless inbound claim mapping is
        // turned off, so reading only one of them would work on exactly one configuration.
        var subject = CreateResolver().Resolve(ClaimsPrincipalOrNone.User((ClaimTypes.NameIdentifier, "u1")).Principal);

        Assert.Equal("u1", subject!.Id);
    }

    [Fact]
    public void Resolve_BothClaims_PrefersSub()
    {
        var subject = CreateResolver().Resolve(ClaimsPrincipalOrNone.User((ClaimTypes.NameIdentifier, "mapped"), ("sub", "raw")).Principal);

        Assert.Equal("raw", subject!.Id);
    }

    [Fact]
    public void Resolve_AnonymousPrincipal_IsNull()
    {
        Assert.Null(CreateResolver().Resolve(ClaimsPrincipalOrNone.Anonymous().Principal));
    }

    [Fact]
    public void Resolve_NullPrincipal_IsNull()
    {
        Assert.Null(CreateResolver().Resolve(null));
    }

    [Fact]
    public void Resolve_MachineTokenWithNoSubject_IsNull()
    {
        Assert.Null(CreateResolver().Resolve(ClaimsPrincipalOrNone.User(("client_id", "svc"), ("scope", "billing.read")).Principal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_BlankSubjectClaim_IsNull(string value)
    {
        Assert.Null(CreateResolver().Resolve(ClaimsPrincipalOrNone.User(("sub", value)).Principal));
    }

    [Fact]
    public void Resolve_ConfiguredAttributeClaim_IsLiftedOntoTheSubject()
    {
        // This is how an identity becomes composite without the library knowing what an
        // organisation is.
        var resolver = CreateResolver(options => options.Subject.AttributeClaims["org_id"] = "organization");

        var subject = resolver.Resolve(ClaimsPrincipalOrNone.User(("sub", "u1"), ("org_id", "acme")).Principal);

        Assert.Equal("acme", subject!.Attributes["organization"]);
    }

    [Fact]
    public void Resolve_AttributeClaimAbsent_LeavesTheAttributeOff()
    {
        var resolver = CreateResolver(options => options.Subject.AttributeClaims["org_id"] = "organization");

        var subject = resolver.Resolve(ClaimsPrincipalOrNone.User(("sub", "u1")).Principal);

        Assert.Empty(subject!.Attributes);
    }

    [Fact]
    public void Resolve_SameSubjectInDifferentOrganisations_AreNotTheSameSubject()
    {
        var resolver = CreateResolver(options => options.Subject.AttributeClaims["org_id"] = "organization");

        var first = resolver.Resolve(ClaimsPrincipalOrNone.User(("sub", "u1"), ("org_id", "acme")).Principal);
        var second = resolver.Resolve(ClaimsPrincipalOrNone.User(("sub", "u1"), ("org_id", "other")).Principal);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Resolve_CustomIdClaimType_IsHonoured()
    {
        var resolver = CreateResolver(options =>
        {
            options.Subject.IdClaimTypes.Clear();
            options.Subject.IdClaimTypes.Add("user_id");
        });

        Assert.Equal("u1", resolver.Resolve(ClaimsPrincipalOrNone.User(("user_id", "u1")).Principal)!.Id);
        Assert.Null(resolver.Resolve(ClaimsPrincipalOrNone.User(("sub", "u1")).Principal));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new ClaimsAccessSubjectResolver(null!));
    }

    /// <summary>
    /// An options monitor over one fixed value.
    /// </summary>
    private sealed class StaticOptionsMonitor : IOptionsMonitor<AccessAuthorizationOptions>
    {
        public StaticOptionsMonitor(AccessAuthorizationOptions value)
        {
            this.CurrentValue = value;
        }

        public AccessAuthorizationOptions CurrentValue { get; }

        public AccessAuthorizationOptions Get(string? name)
        {
            return this.CurrentValue;
        }

        public IDisposable OnChange(Action<AccessAuthorizationOptions, string?> listener)
        {
            return new NoOpDisposable();
        }

        private sealed class NoOpDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

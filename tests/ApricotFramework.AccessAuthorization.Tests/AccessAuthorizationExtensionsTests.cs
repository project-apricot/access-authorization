using ApricotFramework.AccessAuthorization.Extensions;
using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessAuthorizationExtensionsTests
{
    private static readonly AccessContext Context = AccessContext.For(AccessSubject.Create("u1"));

    private static DefaultAccessAuthorization Granting(params string[] accesses)
    {
        return new DefaultAccessAuthorization([new AssignedAccessRule(new CountingAccessResolver(accesses))]);
    }

    [Fact]
    public async Task CheckAnyAsync_OneOfSeveralGranted_IsTrue()
    {
        Assert.True(await Granting("a").CheckAnyAsync(Context, ["a", "b"], Ct.Token));
    }

    [Fact]
    public async Task CheckAnyAsync_NoneGranted_IsFalse()
    {
        Assert.False(await Granting("c").CheckAnyAsync(Context, ["a", "b"], Ct.Token));
    }

    [Fact]
    public async Task CheckAllAsync_EveryOneGranted_IsTrue()
    {
        Assert.True(await Granting("a", "b").CheckAllAsync(Context, ["a", "b"], Ct.Token));
    }

    [Fact]
    public async Task CheckAllAsync_OneMissing_IsFalse()
    {
        Assert.False(await Granting("a").CheckAllAsync(Context, ["a", "b"], Ct.Token));
    }

    [Fact]
    public async Task CheckAllAsync_NoRequirements_IsFalse()
    {
        // The predecessor returned true here, so an attribute declaring no accesses admitted
        // everyone. Vacuous truth is the wrong default for a gate.
        Assert.False(await Granting("a").CheckAllAsync(Context, [], Ct.Token));
    }

    [Fact]
    public async Task CheckAnyAsync_NoRequirements_IsFalse()
    {
        Assert.False(await Granting("a").CheckAnyAsync(Context, [], Ct.Token));
    }

    [Fact]
    public async Task CheckAllAsync_DuplicateRequirements_IsSatisfiedOnce()
    {
        Assert.True(await Granting("a").CheckAllAsync(Context, ["a", "a"], Ct.Token));
    }

    [Fact]
    public async Task RequireAnyAsync_NoneGranted_ThrowsNamingEverythingAsked()
    {
        var exception = await Assert.ThrowsAsync<AccessAuthorizationException>(() => Granting("c").RequireAnyAsync(Context, ["a", "b"], Ct.Token));

        Assert.Equal(["a", "b"], exception.MissingAccesses.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task RequireAllAsync_OneMissing_ThrowsNamingOnlyTheMissingOne()
    {
        var exception = await Assert.ThrowsAsync<AccessAuthorizationException>(() => Granting("a").RequireAllAsync(Context, ["a", "b"], Ct.Token));

        Assert.Equal(["b"], exception.MissingAccesses);
    }

    [Fact]
    public async Task RequireAllAsync_Satisfied_DoesNotThrow()
    {
        await Granting("a", "b").RequireAllAsync(Context, ["a", "b"], Ct.Token);
    }

    [Fact]
    public async Task RequireAllAsync_NoRequirements_Throws()
    {
        await Assert.ThrowsAsync<AccessAuthorizationException>(() => Granting("a").RequireAllAsync(Context, [], Ct.Token));
    }

    [Theory]
    [InlineData(RequirementMatch.Any, true)]
    [InlineData(RequirementMatch.All, false)]
    public async Task CheckAsync_MatchMode_SelectsTheRightSemantics(RequirementMatch match, bool expected)
    {
        Assert.Equal(expected, await Granting("a").CheckAsync(Context, ["a", "b"], match, Ct.Token));
    }

    [Fact]
    public async Task GetAllowedAsync_AndTheGate_NeverDisagree()
    {
        // The one invariant the whole design exists to guarantee: an access a capability listing
        // reports is an access the gate will accept. Both run the same pipeline, so this test's job
        // is to catch a future refactor that reintroduces two paths.
        var owned = AccessResource.Create("sales:order", "42", new Dictionary<string, object?> { ["ownerId"] = "u1" });
        var foreign = AccessResource.Create("sales:order", "43", new Dictionary<string, object?> { ["ownerId"] = "u2" });

        var authorization = new DefaultAccessAuthorization(
        [
            new FixedRule(AccessDecision.Deny, "sales:orders:archive"),
            new AssignedAccessRule(new CountingAccessResolver("sales:orders:read")),
            new OwnerRule("sales:orders:edit"),
        ]);

        var candidates = new[] { "sales:orders:read", "sales:orders:edit", "sales:orders:archive", "sales:orders:unknown" };

        foreach (var subject in new[] { AccessSubject.Create("u1"), AccessSubject.Create("u2") })
        {
            foreach (var resource in new AccessResource?[] { null, owned, foreign })
            {
                var context = AccessContext.For(subject, resource);
                var evaluation = await authorization.GetAllowedAsync(context, candidates, Ct.Token);

                foreach (var access in candidates)
                {
                    Assert.Equal(evaluation.Contains(access), await authorization.CheckAllAsync(context, [access], Ct.Token));
                    Assert.Equal(evaluation.Contains(access), await authorization.CheckAnyAsync(context, [access], Ct.Token));
                }
            }
        }
    }

    /// <summary>
    /// A resource-attribute rule: the owner of an instance may act on it.
    /// </summary>
    private sealed class OwnerRule : AccessRule
    {
        private readonly string access;

        public OwnerRule(string access)
        {
            this.access = access;
        }

        public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
        {
            if (!string.Equals(access, this.access, StringComparison.Ordinal) || context.Resource is null)
            {
                return Task.FromResult(AccessDecision.Abstain);
            }

            var owner = context.Resource.Attributes.GetValueOrDefault("ownerId") as string;

            return Task.FromResult(string.Equals(owner, context.Subject.Id, StringComparison.Ordinal) ? AccessDecision.Allow : AccessDecision.Abstain);
        }
    }
}

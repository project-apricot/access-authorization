using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.Tests;

public class DefaultAccessAuthorizationTests
{
    private static readonly AccessContext Context = AccessContext.For(AccessSubject.Create("u1"));

    private static HashSet<string> Candidates(params string[] values)
    {
        return new HashSet<string>(values, StringComparer.Ordinal);
    }

    [Fact]
    public async Task GetAllowedAsync_NoRules_AllowsNothing()
    {
        var authorization = new DefaultAccessAuthorization([]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a", "b"), Ct.Token);

        Assert.Empty(result.Allowed);
    }

    [Fact]
    public async Task GetAllowedAsync_NoCandidates_AllowsNothing()
    {
        var authorization = new DefaultAccessAuthorization([new AllowAllRule()]);

        var result = await authorization.GetAllowedAsync(Context, Candidates(), Ct.Token);

        Assert.Empty(result.Allowed);
    }

    [Fact]
    public async Task GetAllowedAsync_OnlyCandidatesAreConsidered()
    {
        var authorization = new DefaultAccessAuthorization([new AllowAllRule()]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a"), Ct.Token);

        Assert.Equal(["a"], result.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAllowedAsync_EarlierDeny_IsNotOverriddenByLaterAllow()
    {
        var authorization = new DefaultAccessAuthorization([new FixedRule(AccessDecision.Deny, "a"), new AllowAllRule()]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a", "b"), Ct.Token);

        Assert.Equal(["b"], result.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAllowedAsync_EarlierAllow_ShortCircuitsLaterRules()
    {
        var later = new FixedRule(AccessDecision.Deny, "a");
        var authorization = new DefaultAccessAuthorization([new FixedRule(AccessDecision.Allow, "a"), later]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a"), Ct.Token);

        Assert.Equal(["a"], result.Allowed.Order(StringComparer.Ordinal));
        Assert.Equal(0, later.Calls);
    }

    [Fact]
    public async Task GetAllowedAsync_AbstainingRule_ChangesNothing()
    {
        var authorization = new DefaultAccessAuthorization([new FixedRule(AccessDecision.Allow, "unrelated"), new FixedRule(AccessDecision.Allow, "a")]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a"), Ct.Token);

        Assert.Equal(["a"], result.Allowed.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetAllowedAsync_LaterRuleSeesOnlyUndecidedAccesses()
    {
        var later = new FixedRule(AccessDecision.Allow, "a", "b");
        var authorization = new DefaultAccessAuthorization([new FixedRule(AccessDecision.Allow, "a"), later]);

        await authorization.GetAllowedAsync(Context, Candidates("a", "b"), Ct.Token);

        // "a" was settled by the first rule, so the second is asked about "b" alone.
        Assert.Equal(1, later.Calls);
    }

    [Fact]
    public async Task GetAllowedAsync_RuleContradictingItself_Denies()
    {
        var authorization = new DefaultAccessAuthorization([new ContradictingRule("a"), new AllowAllRule()]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("a"), Ct.Token);

        Assert.Empty(result.Allowed);
    }

    [Fact]
    public async Task GetAllowedAsync_FailingRule_PropagatesInsteadOfDenying()
    {
        // An unreachable attribute source is not the same answer as "not permitted"; reporting it as
        // one would turn an outage into a silent denial nobody notices.
        var authorization = new DefaultAccessAuthorization([new ThrowingRule()]);

        await Assert.ThrowsAsync<InvalidOperationException>(() => authorization.GetAllowedAsync(Context, Candidates("a"), Ct.Token));
    }

    [Fact]
    public async Task GetAllowedAsync_Always_ComparesAccessesOrdinally()
    {
        var authorization = new DefaultAccessAuthorization([new FixedRule(AccessDecision.Allow, "sales:orders:read")]);

        var result = await authorization.GetAllowedAsync(Context, Candidates("Content:Orders:Read"), Ct.Token);

        Assert.Empty(result.Allowed);
    }

    [Fact]
    public async Task GetAllowedAsync_NullArguments_Throw()
    {
        var authorization = new DefaultAccessAuthorization([]);

        await Assert.ThrowsAsync<ArgumentNullException>(() => authorization.GetAllowedAsync(null!, Candidates("a"), Ct.Token));
        await Assert.ThrowsAsync<ArgumentNullException>(() => authorization.GetAllowedAsync(Context, null!, Ct.Token));
    }
}

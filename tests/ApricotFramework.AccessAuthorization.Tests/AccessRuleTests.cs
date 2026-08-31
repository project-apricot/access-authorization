namespace ApricotFramework.AccessAuthorization.Tests;

public class AccessRuleTests
{
    private static readonly AccessContext Context = AccessContext.For(AccessSubject.Create("u1"));

    [Fact]
    public async Task EvaluateManyAsync_Default_AgreesWithEvaluateAsync()
    {
        // The batched form is an optimisation, never a second opinion. A rule whose two forms
        // disagree makes a listing and a gate disagree.
        var candidates = new HashSet<string>(["a", "b", "c", "d"], StringComparer.Ordinal);
        var rules = new AccessRule[]
        {
            new FixedRule(AccessDecision.Allow, "a", "c"),
            new FixedRule(AccessDecision.Deny, "b"),
            new AllowAllRule(),
        };

        foreach (var rule in rules)
        {
            var grants = await rule.EvaluateManyAsync(Context, candidates, Ct.Token);

            foreach (var access in candidates)
            {
                var decision = await rule.EvaluateAsync(Context, access, Ct.Token);

                Assert.Equal(decision == AccessDecision.Allow, grants.Allowed.Contains(access));
                Assert.Equal(decision == AccessDecision.Deny, grants.Denied.Contains(access));
            }
        }
    }

    [Fact]
    public async Task EvaluateManyAsync_NothingDecided_ReturnsNone()
    {
        var rule = new FixedRule(AccessDecision.Allow, "unrelated");

        var grants = await rule.EvaluateManyAsync(Context, new HashSet<string>(["a"], StringComparer.Ordinal), Ct.Token);

        Assert.Same(AccessRuleGrants.None, grants);
    }
}

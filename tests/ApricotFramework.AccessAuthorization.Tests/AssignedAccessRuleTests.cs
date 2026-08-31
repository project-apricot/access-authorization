using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.Tests;

public class AssignedAccessRuleTests
{
    private static readonly AccessSubject Subject = AccessSubject.Create("u1");

    [Fact]
    public async Task EvaluateAsync_AssignedAccess_Allows()
    {
        var rule = new AssignedAccessRule(new CountingAccessResolver("a"));

        Assert.Equal(AccessDecision.Allow, await rule.EvaluateAsync(AccessContext.For(Subject), "a", Ct.Token));
    }

    [Fact]
    public async Task EvaluateAsync_UnassignedAccess_Abstains()
    {
        var rule = new AssignedAccessRule(new CountingAccessResolver("a"));

        // Abstain rather than Deny, so a later rule can still grant it.
        Assert.Equal(AccessDecision.Abstain, await rule.EvaluateAsync(AccessContext.For(Subject), "b", Ct.Token));
    }

    [Fact]
    public async Task EvaluateAsync_ResourceSupplied_IsIgnored()
    {
        // An assigned access is a statement about the type, not about one instance. Narrowing it to
        // an instance is a rule of the application's own.
        var rule = new AssignedAccessRule(new CountingAccessResolver("sales:orders:edit"));
        var context = AccessContext.For(Subject, AccessResource.Create("sales:order", "42"));

        Assert.Equal(AccessDecision.Allow, await rule.EvaluateAsync(context, "sales:orders:edit", Ct.Token));
    }

    [Fact]
    public async Task EvaluateManyAsync_ManyAccesses_ResolvesOnce()
    {
        var resolver = new CountingAccessResolver("a", "b");
        var rule = new AssignedAccessRule(resolver);
        var candidates = new HashSet<string>(Enumerable.Range(0, 60).Select(index => $"access:{index}").Append("a").Append("b"), StringComparer.Ordinal);

        var grants = await rule.EvaluateManyAsync(AccessContext.For(Subject), candidates, Ct.Token);

        Assert.Equal(1, resolver.Calls);
        Assert.Equal(["a", "b"], grants.Allowed.Order(StringComparer.Ordinal));
        Assert.Empty(grants.Denied);
    }

    [Fact]
    public async Task EvaluateManyAsync_NoAccesses_ResolvesNothing()
    {
        var resolver = new CountingAccessResolver("a");
        var rule = new AssignedAccessRule(resolver);

        var grants = await rule.EvaluateManyAsync(AccessContext.For(Subject), new HashSet<string>(StringComparer.Ordinal), Ct.Token);

        Assert.Equal(0, resolver.Calls);
        Assert.Empty(grants.Allowed);
    }
}

namespace ApricotFramework.AccessAuthorization.Tests;

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
/// A resolver returning a fixed set and counting how often it was asked, so a batched rule can be
/// shown to make one round trip.
/// </summary>
internal sealed class CountingAccessResolver : IAccessResolver
{
    private readonly IReadOnlySet<string> accesses;

    public CountingAccessResolver(params string[] accesses)
    {
        this.accesses = new HashSet<string>(accesses, StringComparer.Ordinal);
    }

    public int Calls { get; private set; }

    public Task<IReadOnlySet<string>> ResolveAsync(AccessSubject subject, CancellationToken cancellationToken = default)
    {
        this.Calls++;

        return Task.FromResult(this.accesses);
    }
}

/// <summary>
/// A rule returning a fixed decision for named accesses, recording that it ran.
/// </summary>
internal sealed class FixedRule : AccessRule
{
    private readonly AccessDecision decision;

    private readonly HashSet<string> accesses;

    public FixedRule(AccessDecision decision, params string[] accesses)
    {
        this.decision = decision;
        this.accesses = new HashSet<string>(accesses, StringComparer.Ordinal);
    }

    public int Calls { get; private set; }

    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        this.Calls++;

        return Task.FromResult(this.accesses.Contains(access) ? this.decision : AccessDecision.Abstain);
    }
}

/// <summary>
/// A rule that allows everything it is asked about, standing in for a bypass.
/// </summary>
internal sealed class AllowAllRule : AccessRule
{
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessDecision.Allow);
    }
}

/// <summary>
/// A rule that fails, standing in for an attribute source that is unreachable.
/// </summary>
internal sealed class ThrowingRule : AccessRule
{
    public override Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("the attribute source is unreachable");
    }
}

/// <summary>
/// A rule that names one access in both its allowed and denied sets, which must fail closed.
/// </summary>
internal sealed class ContradictingRule : IAccessRule
{
    private readonly string access;

    public ContradictingRule(string access)
    {
        this.access = access;
    }

    public Task<AccessDecision> EvaluateAsync(AccessContext context, string access, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AccessDecision.Deny);
    }

    public Task<AccessRuleGrants> EvaluateManyAsync(AccessContext context, IReadOnlySet<string> accesses, CancellationToken cancellationToken = default)
    {
        var single = new HashSet<string>([this.access], StringComparer.Ordinal);

        return Task.FromResult(new AccessRuleGrants(single, single));
    }
}

/// <summary>
/// The ambient test cancellation token, so async calls stay responsive to a cancelled run.
/// </summary>
internal static class Ct
{
    internal static CancellationToken Token => TestContext.Current.CancellationToken;
}

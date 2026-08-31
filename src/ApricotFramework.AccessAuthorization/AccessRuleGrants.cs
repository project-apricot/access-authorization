namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// What a rule decided across several accesses at once.
/// </summary>
/// <remarks>
/// Accesses in neither set are abstained on. An access in both is denied — a rule contradicting
/// itself fails closed.
/// </remarks>
public sealed class AccessRuleGrants
{
    /// <summary>
    /// Creates a new set of grants.
    /// </summary>
    /// <param name="allowed">The accesses granted, or <see langword="null"/> for none.</param>
    /// <param name="denied">The accesses refused, or <see langword="null"/> for none.</param>
    public AccessRuleGrants(IReadOnlySet<string>? allowed, IReadOnlySet<string>? denied)
    {
        this.Allowed = allowed ?? AccessSets.Empty;
        this.Denied = denied ?? AccessSets.Empty;
    }

    /// <summary>
    /// Gets grants deciding nothing.
    /// </summary>
    public static AccessRuleGrants None { get; } = new AccessRuleGrants(null, null);

    /// <summary>
    /// Gets the accesses this rule grants.
    /// </summary>
    public IReadOnlySet<string> Allowed { get; }

    /// <summary>
    /// Gets the accesses this rule refuses.
    /// </summary>
    public IReadOnlySet<string> Denied { get; }
}

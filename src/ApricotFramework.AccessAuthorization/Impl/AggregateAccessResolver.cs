namespace ApricotFramework.AccessAuthorization.Impl;

/// <summary>
/// Unions every registered store into one ordinal set.
/// </summary>
public class AggregateAccessResolver : IAccessResolver
{
    /// <summary>
    /// The stores to union.
    /// </summary>
    private readonly IEnumerable<IAccessStore> stores;

    /// <summary>
    /// Creates a new resolver.
    /// </summary>
    /// <param name="stores">The stores to union. None registered means nothing is assigned.</param>
    public AggregateAccessResolver(IEnumerable<IAccessStore> stores)
    {
        ArgumentNullException.ThrowIfNull(stores);

        this.stores = stores;
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlySet<string>> ResolveAsync(AccessSubject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var effective = new HashSet<string>(StringComparer.Ordinal);

        foreach (var store in this.stores)
        {
            var assigned = await store.GetAccessesAsync(subject, cancellationToken).ConfigureAwait(false);

            effective.UnionWith(assigned);
        }

        return effective;
    }
}

using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.Tests;

public class AggregateAccessResolverTests
{
    private static readonly AccessSubject Subject = AccessSubject.Create("u1");

    [Fact]
    public async Task ResolveAsync_NoStores_ReturnsEmpty()
    {
        var resolver = new AggregateAccessResolver([]);

        Assert.Empty(await resolver.ResolveAsync(Subject, Ct.Token));
    }

    [Fact]
    public async Task ResolveAsync_SeveralStores_UnionsThem()
    {
        // Direct grants, group inheritance and role expansion are separate sources that add up,
        // which is what the predecessor's single UNION query did inside the database.
        var resolver = new AggregateAccessResolver([new CountingAccessStore("a"), new CountingAccessStore("b", "a"), new CountingAccessStore()]);

        var effective = await resolver.ResolveAsync(Subject, Ct.Token);

        Assert.Equal(["a", "b"], effective.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task ResolveAsync_StoreUsingLooseComparer_DoesNotWidenGrants()
    {
        var resolver = new AggregateAccessResolver([new CaseInsensitiveStore("Content:Orders:Read")]);

        var effective = await resolver.ResolveAsync(Subject, Ct.Token);

        Assert.DoesNotContain("sales:orders:read", effective);
    }

    [Fact]
    public async Task ResolveAsync_NullSubject_Throws()
    {
        var resolver = new AggregateAccessResolver([]);

        await Assert.ThrowsAsync<ArgumentNullException>(() => resolver.ResolveAsync(null!, Ct.Token));
    }

    /// <summary>
    /// A store whose own set ignores case, to prove the resolver does not adopt its comparer.
    /// </summary>
    private sealed class CaseInsensitiveStore : IAccessStore
    {
        private readonly IReadOnlySet<string> accesses;

        public CaseInsensitiveStore(params string[] accesses)
        {
            this.accesses = new HashSet<string>(accesses, StringComparer.OrdinalIgnoreCase);
        }

        public Task<IReadOnlySet<string>> GetAccessesAsync(AccessSubject subject, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.accesses);
        }
    }
}

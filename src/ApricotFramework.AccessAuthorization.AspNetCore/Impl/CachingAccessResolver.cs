using ApricotFramework.AccessAuthorization.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Impl;

/// <summary>
/// Reuses a subject's effective assigned accesses for a configured lifetime.
/// </summary>
/// <remarks>
/// Wraps whatever resolver it is given, so it is unaffected by where the accesses come from — the
/// predecessor's cache was welded to one SQL repository. Caching is off unless a lifetime is
/// configured, because a cached grant outlives a revocation by up to that long.
/// </remarks>
public class CachingAccessResolver : IAccessResolver
{
    /// <summary>
    /// Namespaces the cache entries, so they cannot collide with anything else sharing the cache.
    /// </summary>
    private const string CacheKeyPrefix = "ApricotFramework.AccessAuthorization.Effective:";

    /// <summary>
    /// The resolver actually doing the work.
    /// </summary>
    private readonly IAccessResolver inner;

    /// <summary>
    /// The cache.
    /// </summary>
    private readonly IMemoryCache cache;

    /// <summary>
    /// The settings, read per call so a lifetime change takes effect without a restart.
    /// </summary>
    private readonly IOptionsMonitor<AccessAuthorizationOptions> options;

    /// <summary>
    /// Creates a new caching resolver.
    /// </summary>
    /// <param name="inner">The resolver actually doing the work.</param>
    /// <param name="cache">The cache.</param>
    /// <param name="options">The settings.</param>
    public CachingAccessResolver(IAccessResolver inner, IMemoryCache cache, IOptionsMonitor<AccessAuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);

        this.inner = inner;
        this.cache = cache;
        this.options = options;
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlySet<string>> ResolveAsync(AccessSubject subject, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subject);

        var lifetime = this.options.CurrentValue.CacheLifetime;

        if (lifetime is null || lifetime <= TimeSpan.Zero)
        {
            return await this.inner.ResolveAsync(subject, cancellationToken).ConfigureAwait(false);
        }

        var key = CacheKeyPrefix + subject.GetKey();

        if (this.cache.TryGetValue(key, out IReadOnlySet<string>? cached) && cached is not null)
        {
            return cached;
        }

        var resolved = await this.inner.ResolveAsync(subject, cancellationToken).ConfigureAwait(false);

        return this.cache.Set(key, resolved, lifetime.Value);
    }
}

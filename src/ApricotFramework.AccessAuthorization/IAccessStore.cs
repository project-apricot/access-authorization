namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// A source of the accesses statically assigned to a subject.
/// </summary>
/// <remarks>
/// Several may be registered and their results are unioned, so direct grants, group inheritance and
/// role expansion can each be their own store. Lookups are keyed by subject only, because the
/// resulting set is what gets cached; anything that has to consider the resource is a rule.
/// </remarks>
public interface IAccessStore
{
    /// <summary>
    /// Gets the accesses this store assigns to a subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The assigned accesses, empty when none.</returns>
    Task<IReadOnlySet<string>> GetAccessesAsync(AccessSubject subject, CancellationToken cancellationToken = default);
}

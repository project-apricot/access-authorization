namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Turns the registered stores into one effective set of assigned accesses.
/// </summary>
/// <remarks>
/// Replace this to change how sources combine — to expand a hierarchy, or to compute the set some
/// other way entirely. It is also where caching is applied, since the set is a function of the
/// subject alone.
/// </remarks>
public interface IAccessResolver
{
    /// <summary>
    /// Resolves the effective assigned accesses for a subject.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The effective accesses, empty when none.</returns>
    Task<IReadOnlySet<string>> ResolveAsync(AccessSubject subject, CancellationToken cancellationToken = default);
}

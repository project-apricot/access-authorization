namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// The decision point. One method, because a gate and a capability listing are the same question
/// over different candidate sets, and two code paths would eventually disagree.
/// </summary>
/// <remarks>
/// Use the extension methods in <c>AccessAuthorizationExtensions</c> for the ordinary shapes.
/// Replacing this service delegates every decision elsewhere — to a remote decision point, say —
/// while the attributes, handlers and imperative calls above it stay unchanged.
/// </remarks>
public interface IAccessAuthorization
{
    /// <summary>
    /// Determines which of the candidate accesses are allowed in a context.
    /// </summary>
    /// <param name="context">The subject, resource and environment.</param>
    /// <param name="candidates">The accesses to decide. Nothing outside this set is considered.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed subset of the candidates.</returns>
    Task<AccessEvaluation> GetAllowedAsync(AccessContext context, IReadOnlySet<string> candidates, CancellationToken cancellationToken = default);
}

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// The accesses allowed out of those asked about.
/// </summary>
/// <remarks>
/// A type rather than a bare set, so a decision can later carry obligations, per-access reasons or a
/// cache lifetime from a remote decision point without breaking callers.
/// </remarks>
public sealed class AccessEvaluation
{
    /// <summary>
    /// Creates a new evaluation.
    /// </summary>
    /// <param name="allowed">Allowed accesses.</param>
    public AccessEvaluation(IEnumerable<string>? allowed)
    {
        this.Allowed = AccessSets.ToFrozenOrdinalSet(allowed);
    }

    /// <summary>
    /// Gets an evaluation allowing nothing.
    /// </summary>
    public static AccessEvaluation Empty { get; } = new(null);

    /// <summary>
    /// Gets the allowed accesses. Anything asked about and absent here is denied.
    /// </summary>
    public IReadOnlySet<string> Allowed { get; }

    /// <summary>
    /// Checks whether one access is allowed.
    /// </summary>
    /// <param name="access">The access to check.</param>
    /// <returns><see langword="true"/> when the access is allowed.</returns>
    public bool Contains(string access)
    {
        ArgumentNullException.ThrowIfNull(access);

        return this.Allowed.Contains(access);
    }
}

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Everything a decision is made from, except the access being asked about.
/// </summary>
/// <remarks>
/// Subject, resource, and environment are the attribute-based access control triple; the access
/// string a rule is handed is the action. New dimensions can be added here without changing any
/// rule signature.
/// </remarks>
public sealed class AccessContext
{
    /// <summary>
    /// Creates a new context.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="resource">The resource, or <see langword="null"/> for none.</param>
    /// <param name="environment">The environment, or <see langword="null"/> for an empty one.</param>
    private AccessContext(AccessSubject subject, AccessResource? resource, AccessEnvironment? environment)
    {
        this.Subject = subject;
        this.Resource = resource;
        this.Environment = environment ?? AccessEnvironment.Empty;
    }

    /// <summary>
    /// Gets the subject the decision is about.
    /// </summary>
    public AccessSubject Subject { get; }

    /// <summary>
    /// Gets the resource the decision is about, or <see langword="null"/> when no particular object
    /// is involved.
    /// </summary>
    public AccessResource? Resource { get; }

    /// <summary>
    /// Gets attributes of the surrounding request rather than of either identity. Never null.
    /// </summary>
    public AccessEnvironment Environment { get; }

    /// <summary>
    /// Creates a context for a subject acting on a resource in a given environment.
    /// </summary>
    /// <param name="subject">The subject.</param>
    /// <param name="resource">The resource, or <see langword="null"/> for none.</param>
    /// <param name="environment">The environment, or <see langword="null"/> for an empty one.</param>
    /// <returns>The context.</returns>
    public static AccessContext For(AccessSubject subject, AccessResource? resource = null, AccessEnvironment? environment = null)
    {
        ArgumentNullException.ThrowIfNull(subject);

        return new AccessContext(subject, resource, environment);
    }
}

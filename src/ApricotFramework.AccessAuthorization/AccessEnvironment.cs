namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Attributes of the surrounding request rather than of either identity — the caller's address, the
/// time of day, whatever a rule needs that is neither the subject nor the object.
/// </summary>
/// <remarks>
/// A type rather than a bare dictionary, for the same reason <see cref="AccessEvaluation"/> is: a
/// dictionary freezes the shape, and a strongly typed member could never be added later without
/// breaking callers.
/// </remarks>
public sealed class AccessEnvironment
{
    /// <summary>
    /// Creates a new environment.
    /// </summary>
    /// <param name="attributes">The attributes.</param>
    private AccessEnvironment(IReadOnlyDictionary<string, object?>? attributes)
    {
        this.Attributes = AccessAttributes.Normalize(attributes, nameof(attributes));
    }

    /// <summary>
    /// Gets an environment carrying nothing.
    /// </summary>
    public static AccessEnvironment Empty { get; } = new AccessEnvironment(null);

    /// <summary>
    /// Gets the environment attributes.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Attributes { get; }

    /// <summary>
    /// Creates an environment from attributes.
    /// </summary>
    /// <param name="attributes">The attributes, or <see langword="null"/> for none.</param>
    /// <returns>The environment.</returns>
    public static AccessEnvironment For(IReadOnlyDictionary<string, object?>? attributes = null)
    {
        return attributes is null || attributes.Count == 0 ? Empty : new AccessEnvironment(attributes);
    }
}

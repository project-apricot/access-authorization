namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// The object a decision is made about: a type, an optional instance id, and attributes describing it.
/// </summary>
/// <remarks>
/// A null <see cref="Id"/> means the type as a whole rather than one instance — "may create orders"
/// as opposed to "may edit order 42". Attributes carry whatever a rule needs to decide, and
/// are loosely typed because they come from your domain rather than from a token.
/// </remarks>
public sealed class AccessResource : IEquatable<AccessResource>
{
    /// <summary>
    /// Creates a new resource.
    /// </summary>
    /// <param name="type">The resource type.</param>
    /// <param name="id">The instance id, or <see langword="null"/> for the type as a whole.</param>
    /// <param name="attributes">The attributes describing the resource.</param>
    private AccessResource(string type, string? id, IReadOnlyDictionary<string, object?>? attributes)
    {
        this.Type = type;
        this.Id = id;
        this.Attributes = AccessAttributes.Normalize(attributes, nameof(attributes));
    }

    /// <summary>
    /// Gets the resource type.
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// Gets the instance id, or <see langword="null"/> when this is the type as a whole.
    /// </summary>
    public string? Id { get; }

    /// <summary>
    /// Gets the attributes describing the resource, such as its owner or its visibility.
    /// </summary>
    /// <remarks>
    /// Values are whatever your domain holds, so a rule pattern matches the type it expects:
    /// <c>if (resource.Attributes["isPublic"] is true)</c>. A value that is present but null is
    /// distinguishable from one that is absent.
    /// </remarks>
    public IReadOnlyDictionary<string, object?> Attributes { get; }

    /// <summary>
    /// Creates a resource standing for a type as a whole, with attributes.
    /// </summary>
    /// <param name="type">The resource type. Cannot be null, empty, or whitespace.</param>
    /// <param name="attributes">The attributes describing the resource.</param>
    /// <returns>The resource.</returns>
    public static AccessResource OfType(string type, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        return new AccessResource(type, null, attributes);
    }

    /// <summary>
    /// Creates a resource standing, for one instance, with attributes.
    /// </summary>
    /// <param name="type">The resource type. Cannot be null, empty, or whitespace.</param>
    /// <param name="id">The instance id. Cannot be null, empty, or whitespace.</param>
    /// <param name="attributes">The attributes describing the resource.</param>
    /// <returns>The resource.</returns>
    /// <remarks>
    /// An identifier you already have — a URN, an ARN, a database key — goes in here verbatim. The
    /// library does not define an identifier scheme of its own.
    /// </remarks>
    public static AccessResource Create(string type, string id, IReadOnlyDictionary<string, object?>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return new AccessResource(type, id, attributes);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Two resources are equal when they name the same object. Attributes describe that object rather
    /// than identify it, so they are not compared — a order reloaded with a changed owner is still the
    /// same order.
    /// </remarks>
    public bool Equals(AccessResource? other)
    {
        return other is not null
            && string.Equals(this.Type, other.Type, StringComparison.Ordinal)
            && string.Equals(this.Id, other.Id, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as AccessResource);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(StringComparer.Ordinal.GetHashCode(this.Type), this.Id is null ? 0 : StringComparer.Ordinal.GetHashCode(this.Id));
    }

    /// <summary>
    /// Returns a readable form for diagnostics, such as <c>sales:order:42</c>.
    /// </summary>
    /// <returns>The readable form.</returns>
    /// <remarks>
    /// For logs and error messages only. It is not collision-proof and is not a cache key.
    /// </remarks>
    public override string ToString()
    {
        return this.Id is null ? this.Type : $"{this.Type}:{this.Id}";
    }
}

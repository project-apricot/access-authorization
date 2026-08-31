using System.Text;

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// The identity a decision is made about: an id, plus any attributes that qualify it.
/// </summary>
/// <remarks>
/// Attributes are what make the identity composite — a user within an organisation is an id with an
/// <c>organization</c> attribute, not a compound string. Equality is by value, so a subject is usable
/// as a dictionary or cache key.
/// </remarks>
public sealed class AccessSubject : IEquatable<AccessSubject>
{
    /// <summary>
    /// The canonical encoding, computed once because equality and hashing both use it.
    /// </summary>
    private readonly string key;

    /// <summary>
    /// Creates a new subject.
    /// </summary>
    /// <param name="id">The subject id.</param>
    /// <param name="attributes">The attributes qualifying the id, or <see langword="null"/> for none.</param>
    private AccessSubject(string id, IReadOnlyDictionary<string, string>? attributes)
    {
        this.Id = id;
        this.Attributes = AccessAttributes.Normalize(attributes, nameof(attributes));
        this.key = BuildKey(this.Id, this.Attributes);
    }

    /// <summary>
    /// Gets the subject id — a user id, a service account id, whatever the host authenticates.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the attributes qualifying the id, such as the organisation the subject is acting in.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; }

    /// <summary>
    /// Creates a subject.
    /// </summary>
    /// <param name="id">The subject id. Cannot be null, empty or whitespace.</param>
    /// <param name="attributes">The attributes qualifying the id, or <see langword="null"/> for none.</param>
    /// <returns>The subject.</returns>
    public static AccessSubject Create(string id, IReadOnlyDictionary<string, string>? attributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return new AccessSubject(id, attributes);
    }

    /// <summary>
    /// Gets the canonical encoding of this subject, suitable as a cache key.
    /// </summary>
    /// <returns>The canonical encoding.</returns>
    /// <remarks>
    /// Distinct subjects always encode distinctly. This is a runtime key, not a persistence format;
    /// do not store it.
    /// </remarks>
    public string GetKey()
    {
        return this.key;
    }

    /// <inheritdoc />
    public bool Equals(AccessSubject? other)
    {
        return other is not null && string.Equals(this.key, other.key, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return this.Equals(obj as AccessSubject);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(this.key);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return this.key;
    }

    /// <summary>
    /// Builds the canonical encoding.
    /// </summary>
    /// <param name="id">The subject id.</param>
    /// <param name="attributes">The normalised attributes.</param>
    /// <returns>The canonical encoding.</returns>
    private static string BuildKey(string id, IReadOnlyDictionary<string, string> attributes)
    {
        var builder = new StringBuilder("sub|");

        AccessAttributes.AppendPart(builder, id);
        AccessAttributes.AppendAttributes(builder, attributes);

        return builder.ToString();
    }
}

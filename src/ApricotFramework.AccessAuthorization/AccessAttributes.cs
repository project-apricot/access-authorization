using System.Collections.ObjectModel;
using System.Text;

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Normalization and canonical encoding shared by the identity types.
/// </summary>
/// <remarks>
/// Parts are length-prefixed rather than delimited, so no attribute value can forge a boundary.
/// A subject id of <c>"a:b"</c> and a subject with an attribute cannot produce the same key.
/// </remarks>
internal static class AccessAttributes
{
    /// <summary>
    /// Separates a part's length from its content.
    /// </summary>
    private const char LengthSeparator = ':';

    /// <summary>
    /// Copies attributes into an ordinal, immutable dictionary, rejecting unusable entries.
    /// </summary>
    /// <param name="attributes">The attributes to copy, or <see langword="null"/> for none.</param>
    /// <param name="parameterName">The caller's parameter name, for exception messages.</param>
    /// <returns>An immutable ordinal view of the attributes.</returns>
    internal static IReadOnlyDictionary<string, string> Normalize(IReadOnlyDictionary<string, string>? attributes, string parameterName)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return ReadOnlyDictionary<string, string>.Empty;
        }

        var copy = new Dictionary<string, string>(attributes.Count, StringComparer.Ordinal);

        foreach (var pair in attributes)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("An attribute name cannot be null or whitespace.", parameterName);
            }

            copy[pair.Key] = pair.Value ?? throw new ArgumentException($"The value of attribute '{pair.Key}' cannot be null.", parameterName);
        }

        return new ReadOnlyDictionary<string, string>(copy);
    }

    /// <summary>
    /// Copies loosely typed attributes into an ordinal, immutable dictionary.
    /// </summary>
    /// <param name="attributes">The attributes to copy, or <see langword="null"/> for none.</param>
    /// <param name="parameterName">The caller's parameter name, for exception messages.</param>
    /// <returns>An immutable ordinal view of the attributes.</returns>
    /// <remarks>
    /// A null value is allowed here, unlike a subject attribute: these describe a domain object, and
    /// "present but unset" is a distinction a rule may care about. They never feed a cache key, which
    /// is why they need no canonical encoding.
    /// </remarks>
    internal static IReadOnlyDictionary<string, object?> Normalize(IReadOnlyDictionary<string, object?>? attributes, string parameterName)
    {
        if (attributes is null || attributes.Count == 0)
        {
            return ReadOnlyDictionary<string, object?>.Empty;
        }

        var copy = new Dictionary<string, object?>(attributes.Count, StringComparer.Ordinal);

        foreach (var pair in attributes)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                throw new ArgumentException("An attribute name cannot be null or whitespace.", parameterName);
            }

            copy[pair.Key] = pair.Value;
        }

        return new ReadOnlyDictionary<string, object?>(copy);
    }

    /// <summary>
    /// Appends a length-prefixed part.
    /// </summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="value">The part, or <see langword="null"/>, which encodes distinctly from empty.</param>
    internal static void AppendPart(StringBuilder builder, string? value)
    {
        if (value is null)
        {
            builder.Append("-1").Append(LengthSeparator);

            return;
        }

        builder.Append(value.Length).Append(LengthSeparator).Append(value);
    }

    /// <summary>
    /// Appends attributes in ordinal key order, so an equal set always encodes identically.
    /// </summary>
    /// <param name="builder">The builder to append to.</param>
    /// <param name="attributes">The attributes to append.</param>
    internal static void AppendAttributes(StringBuilder builder, IReadOnlyDictionary<string, string> attributes)
    {
        builder.Append(attributes.Count).Append(LengthSeparator);

        foreach (var pair in attributes.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            AppendPart(builder, pair.Key);
            AppendPart(builder, pair.Value);
        }
    }
}

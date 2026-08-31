using System.Collections.Frozen;

namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Shared set helpers. Access strings are compared ordinary everywhere, so a store that used a
/// case-insensitive comparer cannot widen a grant.
/// </summary>
internal static class AccessSets
{
    /// <summary>
    /// An empty set that casting cannot mutate.
    /// </summary>
    internal static FrozenSet<string> Empty { get; } = FrozenSet<string>.Empty;

    /// <summary>
    /// Copies a sequence into an ordinal set.
    /// </summary>
    /// <param name="values">The values to copy, or <see langword="null"/> for none.</param>
    /// <returns>An ordinal set of the values.</returns>
    internal static HashSet<string> ToOrdinalSet(IEnumerable<string>? values)
    {
        return values is null ? new HashSet<string>(StringComparer.Ordinal) : new HashSet<string>(values, StringComparer.Ordinal);
    }

    /// <summary>
    /// Copies a sequence into an immutable ordinal set.
    /// </summary>
    /// <param name="values">The values to copy, or <see langword="null"/> for none.</param>
    /// <returns>An immutable ordinal set of the values.</returns>
    internal static FrozenSet<string> ToFrozenOrdinalSet(IEnumerable<string>? values)
    {
        return values is null ? Empty : values.ToFrozenSet(StringComparer.Ordinal);
    }
}

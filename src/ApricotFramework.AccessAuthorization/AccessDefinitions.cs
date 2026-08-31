namespace ApricotFramework.AccessAuthorization;

/// <summary>
/// Validation shared by the catalog implementations.
/// </summary>
internal static class AccessDefinitions
{
    /// <summary>
    /// Rejects a null entry, naming where it came from.
    /// </summary>
    /// <typeparam name="TItem">The entry type.</typeparam>
    /// <param name="items">The entries to check.</param>
    /// <param name="parameterName">The caller's parameter name, for the exception message.</param>
    /// <returns>The entries.</returns>
    /// <exception cref="ArgumentException">An entry was null.</exception>
    /// <remarks>
    /// Declared collections often come from assemblies compiled without nullable annotations, where the
    /// signature guarantees nothing. Failing here points at the registration that supplied the entry;
    /// letting it through fails later inside a capability listing, with nothing to say where it came
    /// from.
    /// </remarks>
    internal static IEnumerable<TItem> WithoutNulls<TItem>(IEnumerable<TItem> items, string parameterName)
        where TItem : class
    {
        var materialised = items as IReadOnlyCollection<TItem> ?? [.. items];

        if (materialised.Any(item => item is null))
        {
            throw new ArgumentException("A declared access definition cannot be null.", parameterName);
        }

        return materialised;
    }
}

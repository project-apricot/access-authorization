using ApricotFramework.AccessAuthorization.Impl;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Impl;

/// <summary>
/// Collects the declared catalogs during registration so one combined catalog can be resolved.
/// </summary>
/// <remarks>
/// Registering each catalog as its own <see cref="IAccessCatalog"/> would mean the container hands a
/// consumer whichever was registered last, silently dropping the rest. Holding them here keeps a
/// single service to resolve, and it is always the union.
/// </remarks>
internal sealed class AccessCatalogRegistry
{
    /// <summary>
    /// Gets the factories for the declared catalogs, in registration order.
    /// </summary>
    internal List<Func<IServiceProvider, IAccessCatalog>> Catalogs { get; } = [];

    /// <summary>
    /// Builds the combined catalog.
    /// </summary>
    /// <param name="provider">The provider to resolve catalogs from.</param>
    /// <returns>The combined catalog.</returns>
    internal IAccessCatalog Build(IServiceProvider provider)
    {
        return new CompositeAccessCatalog(this.Catalogs.Select(factory => factory(provider)));
    }
}

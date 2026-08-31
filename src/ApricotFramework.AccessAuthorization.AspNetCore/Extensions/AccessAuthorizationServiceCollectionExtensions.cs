using ApricotFramework.AccessAuthorization.AspNetCore.Handlers;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Handlers;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Impl;
using ApricotFramework.AccessAuthorization.AspNetCore.Impl;
using ApricotFramework.AccessAuthorization.AspNetCore.Subjects;
using ApricotFramework.AccessAuthorization.AspNetCore.Validation;
using ApricotFramework.AccessAuthorization.Impl;
using ApricotFramework.AccessAuthorization.Options;
using ApricotFramework.AccessAuthorization.Scopes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Extensions;

/// <summary>
/// Registers authorization in a service collection.
/// </summary>
/// <remarks>
/// One call registers both halves. Scope authorization is inert until something asks a scope question,
/// so registering it costs nothing — whereas leaving it out while an endpoint carries a scope attribute
/// produces a requirement no handler can satisfy, and every such request is refused with no
/// explanation anywhere.
/// </remarks>
public static class AccessAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// The configuration section bound when no other is named.
    /// </summary>
    public const string ConfigurationSectionName = "Authorization";

    /// <summary>
    /// Adds access authorization with default settings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return AddAccessAuthorizationCore(services);
    }

    /// <summary>
    /// Adds access authorization, binding its settings from the
    /// <see cref="ConfigurationSectionName"/> configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddAccessAuthorization(configuration, ConfigurationSectionName);
    }

    /// <summary>
    /// Adds access authorization, binding its settings from a named configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The section to bind.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessAuthorization(this IServiceCollection services, IConfiguration configuration, string sectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions<AccessAuthorizationOptions>().Bind(configuration.GetSection(sectionName));

        return AddAccessAuthorizationCore(services);
    }

    /// <summary>
    /// Adds access authorization, configuring its settings in code.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the settings.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessAuthorization(this IServiceCollection services, Action<AccessAuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<AccessAuthorizationOptions>().Configure(configure);

        return AddAccessAuthorizationCore(services);
    }

    /// <summary>
    /// Adds a source of statically assigned accesses.
    /// </summary>
    /// <typeparam name="TStore">The store to add. Its dependencies are injected.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Scoped, because a store normally holds a connection or a unit of work. Several stores are
    /// unioned, so adding one never displaces another. Adding the same type twice is a no-op.
    /// </remarks>
    public static IServiceCollection AddAccessStore<TStore>(this IServiceCollection services)
        where TStore : class, IAccessStore
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAccessStore, TStore>());

        return services;
    }

    /// <summary>
    /// Adds an already-constructed source of statically assigned accesses.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="store">The store to add.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessStore(this IServiceCollection services, IAccessStore store)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(store);

        services.AddSingleton(store);

        return services;
    }

    /// <summary>
    /// Declares the accesses this application knows about.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="accesses">The declared accesses.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Only needed to answer "what is this subject allowed to do?", which cannot be answered without
    /// a candidate set. Gates work without a catalog.
    /// </remarks>
    public static IServiceCollection AddAccessCatalog(this IServiceCollection services, IEnumerable<string> accesses)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(accesses);

        return services.AddAccessCatalog(new StaticAccessCatalog(accesses));
    }

    /// <summary>
    /// Declares the accesses this application knows about, with the resource types they apply to.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="definitions">The declared definitions.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessCatalog(this IServiceCollection services, IEnumerable<AccessDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(definitions);

        return services.AddAccessCatalog(new StaticAccessCatalog(definitions));
    }

    /// <summary>
    /// Adds an already-constructed catalog.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="catalog">The catalog to add.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessCatalog(this IServiceCollection services, IAccessCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(catalog);

        GetCatalogs(services).Catalogs.Add(_ => catalog);

        return services;
    }

    /// <summary>
    /// Adds a catalog resolved from the container.
    /// </summary>
    /// <typeparam name="TCatalog">The catalog to add. Its dependencies are injected.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessCatalog<TCatalog>(this IServiceCollection services)
        where TCatalog : class, IAccessCatalog
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TCatalog>();
        GetCatalogs(services).Catalogs.Add(provider => provider.GetRequiredService<TCatalog>());

        return services;
    }

    /// <summary>
    /// Adds a rule consulted <em>before</em> the assigned-access lookup, which is where a rule that
    /// grants everything to an operator belongs — it then short-circuits the lookup entirely.
    /// </summary>
    /// <typeparam name="TRule">The rule to add. Its dependencies are injected.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessBypassRule<TRule>(this IServiceCollection services)
        where TRule : class, IAccessRule
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<TRule>();
        GetPipeline(services).BypassRules.Add(provider => provider.GetRequiredService<TRule>());

        return services;
    }

    /// <summary>
    /// Adds a rule consulted <em>after</em> the assigned-access lookup, for anything the assigned set
    /// cannot express — a decision that depends on the resource, or on the environment.
    /// </summary>
    /// <typeparam name="TRule">The rule to add. Its dependencies are injected.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    public static IServiceCollection AddAccessRule<TRule>(this IServiceCollection services)
        where TRule : class, IAccessRule
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<TRule>();
        GetPipeline(services).Rules.Add(provider => provider.GetRequiredService<TRule>());

        return services;
    }

    /// <summary>
    /// Registers the pipeline, the decision point and the authorization handler.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    private static IServiceCollection AddAccessAuthorizationCore(IServiceCollection services)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<AccessAuthorizationOptions>, AccessAuthorizationOptionsValidator>());
        services.AddOptions<AccessAuthorizationOptions>().ValidateOnStart();

        services.AddMemoryCache();

        services.TryAddSingleton<IAccessSubjectResolver, ClaimsAccessSubjectResolver>();

        // One catalog service, always the union of everything declared, so two modules can each
        // declare their own accesses without one silently displacing the other.
        var catalogs = GetCatalogs(services);

        services.TryAddSingleton<IAccessCatalog>(catalogs.Build);

        services.TryAddScoped<AggregateAccessResolver>();
        services.TryAddScoped<IAccessResolver>(provider => new CachingAccessResolver(
            provider.GetRequiredService<AggregateAccessResolver>(),
            provider.GetRequiredService<IMemoryCache>(),
            provider.GetRequiredService<IOptionsMonitor<AccessAuthorizationOptions>>()));

        services.TryAddScoped<AssignedAccessRule>();

        var pipeline = GetPipeline(services);

        services.TryAddScoped<IAccessAuthorization>(provider => new DefaultAccessAuthorization(
            pipeline.Build(provider, provider.GetRequiredService<AssignedAccessRule>())));

        services.TryAddEnumerable(ServiceDescriptor.Scoped<IAuthorizationHandler, AccessAuthorizationHandler>());

        // Registered unconditionally: see the remarks on this class for why leaving it to a second
        // call was a trap.
        services.TryAddSingleton<IScopeAuthorization, OptionsAwareScopeAuthorization>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IAuthorizationHandler, ScopeAuthorizationHandler>());

        return services;
    }

    /// <summary>
    /// Gets the catalog registry being assembled, creating it on first use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The registry.</returns>
    private static AccessCatalogRegistry GetCatalogs(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(AccessCatalogRegistry));

        if (existing?.ImplementationInstance is AccessCatalogRegistry registry)
        {
            return registry;
        }

        var created = new AccessCatalogRegistry();

        services.AddSingleton(created);

        return created;
    }

    /// <summary>
    /// Gets the pipeline being assembled, creating it on first use.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The pipeline.</returns>
    /// <remarks>
    /// Held on the collection rather than derived from registration order, so a rule can be added
    /// before or after <c>AddAccessAuthorization</c> and still land in the right stage.
    /// </remarks>
    private static AccessRulePipeline GetPipeline(IServiceCollection services)
    {
        var existing = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(AccessRulePipeline));

        if (existing?.ImplementationInstance is AccessRulePipeline pipeline)
        {
            return pipeline;
        }

        var created = new AccessRulePipeline();

        services.AddSingleton(created);

        return created;
    }
}

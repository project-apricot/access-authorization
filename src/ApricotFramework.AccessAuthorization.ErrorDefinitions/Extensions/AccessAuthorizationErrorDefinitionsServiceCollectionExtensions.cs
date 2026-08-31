using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions.Extensions;

/// <summary>
/// Renders authorization failures as problem details.
/// </summary>
public static class AccessAuthorizationErrorDefinitionsServiceCollectionExtensions
{
    /// <summary>
    /// Describes authorization failures as error definitions, from both an imperative check and an
    /// attribute.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection, for chaining.</returns>
    /// <remarks>
    /// Registers a mapper rather than an exception handler of its own, so one handler answers for every
    /// library. It also decorates the authorization middleware result handler, because a denial from an
    /// attribute raises no exception and would otherwise be a bodiless 403.
    /// </remarks>
    public static IServiceCollection AddAccessAuthorizationErrorDefinitions(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddErrorDefinitions();
        services.AddExceptionErrorMapper<AuthorizationExceptionMapper>();

        AddResultWriter(services);

        return services;
    }

    /// <summary>
    /// Decorates the authorization middleware result handler, once.
    /// </summary>
    /// <param name="services">The service collection.</param>
    private static void AddResultWriter(IServiceCollection services)
    {
        // A marker rather than a search for the writer itself, because it is registered through a
        // factory and the descriptor carries no implementation type to match on.
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ResultWriterRegistered)))
        {
            return;
        }

        services.AddSingleton<ResultWriterRegistered>();

        // Snapshot the handler in place now, so the decorator wraps it rather than itself.
        var existing = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler));

        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(provider =>
            new AccessAuthorizationResultWriter(Resolve(provider, existing)));
    }

    /// <summary>
    /// Resolves the handler that was registered before this one, or the framework default.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="existing">The descriptor in place at registration time, if any.</param>
    /// <returns>The handler to decorate.</returns>
    private static IAuthorizationMiddlewareResultHandler Resolve(IServiceProvider provider, ServiceDescriptor? existing)
    {
        if (existing is null)
        {
            return new AuthorizationMiddlewareResultHandler();
        }

        if (existing.ImplementationInstance is IAuthorizationMiddlewareResultHandler instance)
        {
            return instance;
        }

        if (existing.ImplementationFactory is not null)
        {
            return (IAuthorizationMiddlewareResultHandler)existing.ImplementationFactory(provider);
        }

        return existing.ImplementationType is null
            ? new AuthorizationMiddlewareResultHandler()
            : (IAuthorizationMiddlewareResultHandler)ActivatorUtilities.CreateInstance(provider, existing.ImplementationType);
    }

    /// <summary>
    /// Marks the result writer as already registered.
    /// </summary>
    private sealed class ResultWriterRegistered;
}

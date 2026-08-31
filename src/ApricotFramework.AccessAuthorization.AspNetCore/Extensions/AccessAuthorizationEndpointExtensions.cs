using ApricotFramework.AccessAuthorization.AspNetCore.Attributes;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Attributes;
using Microsoft.AspNetCore.Builder;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Extensions;

/// <summary>
/// Applies the same requirements the attributes carry to endpoints declared in code.
/// </summary>
/// <remarks>
/// These add the very attribute the declarative form uses, so a minimal-API endpoint, a controller
/// action and a gRPC method all reach the decision point by one path.
/// </remarks>
public static class AccessAuthorizationEndpointExtensions
{
    /// <summary>
    /// Requires at least one of the named accesses.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="accesses">The accesses, at least one of which must be granted.</param>
    /// <returns>The builder, for chaining.</returns>
    public static TBuilder RequireAccessAny<TBuilder>(this TBuilder builder, params string[] accesses)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new AuthorizeAnyAccessAttribute(accesses));
    }

    /// <summary>
    /// Requires every one of the named accesses.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="accesses">The accesses, all of which must be granted.</param>
    /// <returns>The builder, for chaining.</returns>
    public static TBuilder RequireAccessAll<TBuilder>(this TBuilder builder, params string[] accesses)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new AuthorizeAllAccessAttribute(accesses));
    }

    /// <summary>
    /// Requires at least one of the named token scopes.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="scopes">The scopes, at least one of which must be present.</param>
    /// <returns>The builder, for chaining.</returns>
    public static TBuilder RequireScopesAny<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new AuthorizeAnyScopesAttribute(scopes));
    }

    /// <summary>
    /// Requires every one of the named token scopes.
    /// </summary>
    /// <typeparam name="TBuilder">The endpoint convention builder.</typeparam>
    /// <param name="builder">The builder.</param>
    /// <param name="scopes">The scopes, all of which must be present.</param>
    /// <returns>The builder, for chaining.</returns>
    public static TBuilder RequireScopesAll<TBuilder>(this TBuilder builder, params string[] scopes)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.RequireAuthorization(new AuthorizeAllScopesAttribute(scopes));
    }
}

using ApricotFramework.AccessAuthorization.AspNetCore.Subjects;
using ApricotFramework.AccessAuthorization.Impl;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Extensions;

/// <summary>
/// Reaches access authorization from a request without injecting three services by hand.
/// </summary>
public static class HttpContextAccessExtensions
{
    /// <summary>
    /// Gets the subject for the current request.
    /// </summary>
    /// <param name="httpContext">The request.</param>
    /// <returns>The subject, or <see langword="null"/> when the caller carries none.</returns>
    public static AccessSubject? GetAccessSubject(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.RequestServices.GetRequiredService<IAccessSubjectResolver>().Resolve(httpContext.User);
    }

    /// <summary>
    /// Gets everything the current caller is allowed to do, optionally on one resource.
    /// </summary>
    /// <param name="httpContext">The request.</param>
    /// <param name="resource">The resource, or <see langword="null"/> for none.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The allowed accesses, empty when the caller carries no subject.</returns>
    /// <exception cref="InvalidOperationException">No access catalog is registered.</exception>
    /// <remarks>
    /// This is what a user interface calls to decide which operations to offer. It runs the same
    /// pipeline as the gates, so an access it lists is one the gates will accept.
    /// </remarks>
    public static Task<AccessEvaluation> GetAllowedAccessesAsync(this HttpContext httpContext, AccessResource? resource = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var subject = httpContext.GetAccessSubject();

        if (subject is null)
        {
            return Task.FromResult(AccessEvaluation.Empty);
        }

        // A missing catalog is a wiring mistake, not an authorization answer. Returning an empty set
        // would hide every operation in the interface and look like a permissions problem.
        var catalog = httpContext.RequestServices.GetRequiredService<IAccessCatalog>();

        if (catalog.GetDefinitions().Count == 0)
        {
            throw new InvalidOperationException(
                "No accesses are declared, so the ones to consider are unknown. Call services.AddAccessCatalog(...) to declare them.");
        }

        var authorization = httpContext.RequestServices.GetRequiredService<IAccessAuthorization>();

        return authorization.GetAllowedAsync(catalog, AccessContext.For(subject, resource), cancellationToken);
    }
}

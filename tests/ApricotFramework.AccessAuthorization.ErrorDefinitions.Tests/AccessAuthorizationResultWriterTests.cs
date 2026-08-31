using System.Text;
using System.Text.Json;
using ApricotFramework.AccessAuthorization.AspNetCore.Requirements;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Requirements;
using ApricotFramework.AccessAuthorization.ErrorDefinitions.Extensions;
using ApricotFramework.ErrorDefinitions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions.Tests;

/// <summary>
/// A denial from an attribute raises no exception, so the exception mapper never sees it. Without this
/// writer the middleware answers a bodyless 403, where the predecessor's exception handler described
/// every denial.
/// </summary>
public class AccessAuthorizationResultWriterTests
{
    /// <summary>
    /// Builds a provider whose innermost handler only sets the status code, standing in for the
    /// framework one without needing an authentication stack.
    /// </summary>
    private static ServiceProvider BuildProvider(int statusCode)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(new StatusOnlyHandler(statusCode));
        services.AddAccessAuthorizationErrorDefinitions();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static async Task<(int Status, string Body, string? ContentType)> DenyAsync(
        ServiceProvider provider, Action<HttpContext>? arrange, params IAuthorizationRequirement[] failed)
    {
        var httpContext = new DefaultHttpContext { RequestServices = provider };
        using var body = new MemoryStream();

        httpContext.Response.Body = body;
        httpContext.Request.Path = "/api/orders/43";

        arrange?.Invoke(httpContext);

        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();
        var result = PolicyAuthorizationResult.Forbid(AuthorizationFailure.Failed(failed));

        await provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(_ => Task.CompletedTask, httpContext, policy, result);

        return (httpContext.Response.StatusCode, Encoding.UTF8.GetString(body.ToArray()), httpContext.Response.ContentType);
    }

    [Fact]
    public async Task HandleAsync_AccessRequirementFailed_WritesProblemDetails()
    {
        using var provider = BuildProvider(403);

        var (status, body, contentType) = await DenyAsync(provider, null, new AccessRequirement(["sales:orders:edit"], RequirementMatch.Any));

        Assert.Equal(403, status);
        Assert.StartsWith("application/problem+json", contentType, StringComparison.Ordinal);

        var first = JsonDocument.Parse(body).RootElement.GetProperty("errors")[0];

        Assert.Equal(ErrorKinds.AccessDenied, first.GetProperty("kind").GetString());
        Assert.Equal(AccessAuthorizationErrors.AccessNotGranted, first.GetProperty("code").GetString());
        Assert.Equal("sales:orders:edit", first.GetProperty("payload").GetProperty("required")[0].GetString());
        Assert.Equal("any", first.GetProperty("payload").GetProperty("match").GetString());
    }

    [Fact]
    public async Task HandleAsync_ScopeRequirementFailed_IsDistinguishableFromAnAccessFailure()
    {
        using var provider = BuildProvider(403);

        var (_, body, _) = await DenyAsync(provider, null, new ScopeRequirement(["sales.read"], RequirementMatch.All));

        var first = JsonDocument.Parse(body).RootElement.GetProperty("errors")[0];

        Assert.Equal(AccessAuthorizationErrors.ScopeNotGranted, first.GetProperty("code").GetString());
        Assert.Equal("all", first.GetProperty("payload").GetProperty("match").GetString());
    }

    [Fact]
    public async Task HandleAsync_UnrelatedRequirementFailed_WritesNothing()
    {
        // Describing a failure this library did not cause would take the body away from whoever owns it.
        using var provider = BuildProvider(403);

        var (_, body, contentType) = await DenyAsync(provider, null, new Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement());

        Assert.Empty(body);
        Assert.Null(contentType);
    }

    [Fact]
    public async Task HandleAsync_Challenged_WritesNothing()
    {
        // A 401 means the caller never said who it was, which is not this library's failure to describe
        // even when one of its requirements is also unmet.
        using var provider = BuildProvider(401);

        var (_, body, _) = await DenyAsync(provider, null, new AccessRequirement(["sales:orders:edit"], RequirementMatch.Any));

        Assert.Empty(body);
    }

    [Fact]
    public async Task HandleAsync_SomethingElseAlreadyWroteABody_LeavesItAlone()
    {
        // The guard that lets this compose with another decorator instead of corrupting the response.
        const string Existing = "{\"written\":\"by someone else\"}";

        using var provider = BuildProvider(403);

        var (_, body, _) = await DenyAsync(
            provider,
            context =>
            {
                context.Response.ContentType = "application/json";
                context.Response.Body.Write(Encoding.UTF8.GetBytes(Existing));
            },
            new AccessRequirement(["sales:orders:edit"], RequirementMatch.Any));

        Assert.Equal(Existing, body);
    }

    [Fact]
    public async Task HandleAsync_Succeeded_WritesNothing()
    {
        using var provider = BuildProvider(200);

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        using var body = new MemoryStream();

        httpContext.Response.Body = body;

        await provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>().HandleAsync(
            _ => Task.CompletedTask,
            httpContext,
            new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build(),
            PolicyAuthorizationResult.Success());

        Assert.Empty(Encoding.UTF8.GetString(body.ToArray()));
    }

    [Fact]
    public void AddAccessAuthorizationErrorDefinitions_CalledTwice_WrapsTheHandlerOnce()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(new StatusOnlyHandler(403));
        services.AddAccessAuthorizationErrorDefinitions();
        services.AddAccessAuthorizationErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler) && descriptor.ImplementationFactory is not null);
        Assert.NotNull(provider.GetRequiredService<IAuthorizationMiddlewareResultHandler>());
    }

    [Fact]
    public async Task HandleAsync_AnotherDecoratorAlreadyInPlace_IsStillReachedThroughIt()
    {
        // Nesting is the expected outcome when a sibling package also decorates: the inner one writes
        // and the outer one stands down, so there is exactly one body either way.
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(new StatusOnlyHandler(403));
        services.AddAccessAuthorizationErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var (_, body, _) = await DenyAsync(provider, null, new AccessRequirement(["a"], RequirementMatch.Any));

        Assert.Contains(AccessAuthorizationErrors.AccessNotGranted, body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Sets the status code the way the framework handler would, and nothing else.
    /// </summary>
    private sealed class StatusOnlyHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly int statusCode;

        public StatusOnlyHandler(int statusCode)
        {
            this.statusCode = statusCode;
        }

        public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            if (!authorizeResult.Succeeded)
            {
                context.Response.StatusCode = this.statusCode;
            }

            return Task.CompletedTask;
        }
    }
}

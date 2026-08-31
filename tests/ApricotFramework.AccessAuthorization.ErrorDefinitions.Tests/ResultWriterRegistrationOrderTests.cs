using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApricotFramework.AccessAuthorization.AspNetCore.Requirements;
using ApricotFramework.AccessAuthorization.ErrorDefinitions.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions.Tests;

/// <summary>
/// The result writer decorates whatever handler is in place, which makes it the one piece of this
/// library whose behaviour could plausibly depend on the order calls are made in. It does not.
/// </summary>
public class ResultWriterRegistrationOrderTests
{
    private static async Task<string> DenyAsync(ServiceProvider provider)
    {
        // A real request runs in a scope, and the authentication stack the framework handler forbids
        // through is scoped.
        using var scope = provider.CreateScope();

        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        using var body = new MemoryStream();

        httpContext.Response.Body = body;
        httpContext.Request.Path = "/api/orders/43";

        var policy = new AuthorizationPolicyBuilder().RequireAssertion(_ => false).Build();
        var result = PolicyAuthorizationResult.Forbid(
            AuthorizationFailure.Failed([new AccessRequirement(["sales:orders:edit"], RequirementMatch.Any)]));

        await scope.ServiceProvider.GetRequiredService<IAuthorizationMiddlewareResultHandler>()
            .HandleAsync(_ => Task.CompletedTask, httpContext, policy, result);

        return Encoding.UTF8.GetString(body.ToArray());
    }

    [Fact]
    public void AddAuthorization_BeforeOrAfter_LeavesTheWriterInPlace()
    {
        // AddAuthorization registers the framework handler with TryAdd, so it cannot displace a writer
        // registered before it, and is wrapped by one registered after it.
        var authorizationFirst = new ServiceCollection();

        authorizationFirst.AddLogging();
        AddForbidScheme(authorizationFirst);
        authorizationFirst.AddAuthorization();
        authorizationFirst.AddAccessAuthorizationErrorDefinitions();

        var writerFirst = new ServiceCollection();

        writerFirst.AddLogging();
        AddForbidScheme(writerFirst);
        writerFirst.AddAccessAuthorizationErrorDefinitions();
        writerFirst.AddAuthorization();

        using var first = authorizationFirst.BuildServiceProvider(validateScopes: true);
        using var second = writerFirst.BuildServiceProvider(validateScopes: true);

        Assert.Equal(
            first.GetRequiredService<IAuthorizationMiddlewareResultHandler>().GetType(),
            second.GetRequiredService<IAuthorizationMiddlewareResultHandler>().GetType());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AnotherDecorator_EitherSide_StillProducesExactlyOneBody(bool siblingFirst)
    {
        // Stands in for ApricotFramework.Authentication.ErrorDefinitions, which decorates the same
        // handler. Both use the same "only if nobody wrote one" guard, so they nest and the inner one
        // wins rather than the response being written twice.
        var services = new ServiceCollection();

        services.AddLogging();
        AddForbidScheme(services);
        services.AddAuthorization();

        if (siblingFirst)
        {
            AddSiblingWriter(services);
            services.AddAccessAuthorizationErrorDefinitions();
        }
        else
        {
            services.AddAccessAuthorizationErrorDefinitions();
            AddSiblingWriter(services);
        }

        using var provider = services.BuildServiceProvider(validateScopes: true);

        var body = await DenyAsync(provider);

        // Whichever wrote it, there is one body and it is not two concatenated documents.
        Assert.Equal(1, CountOccurrences(body, "\"status\""));

        // Order decides which decorator gets there first, and therefore whose description wins.
        // Both are valid problem details; only the detail differs.
        Assert.Contains(siblingFirst ? "GENERIC" : AccessAuthorizationErrors.AccessNotGranted, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnotherDecoratorRegisteredAfter_TheInnerWriterDescribesTheFailure()
    {
        // Registered after ours, the sibling wraps ours; ours runs first and gets to say which
        // requirement failed, which the generic one could not.
        var services = new ServiceCollection();

        services.AddLogging();
        AddForbidScheme(services);
        services.AddAuthorization();
        services.AddAccessAuthorizationErrorDefinitions();
        AddSiblingWriter(services);

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Contains(AccessAuthorizationErrors.AccessNotGranted, await DenyAsync(provider), StringComparison.Ordinal);
    }

    private static void AddForbidScheme(IServiceCollection services)
    {
        services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, StubAuthenticationHandler>("Test", null);
    }

    private static int CountOccurrences(string value, string token)
    {
        var count = 0;
        var index = value.IndexOf(token, StringComparison.Ordinal);

        while (index >= 0)
        {
            count++;
            index = value.IndexOf(token, index + token.Length, StringComparison.Ordinal);
        }

        return count;
    }

    private static void AddSiblingWriter(IServiceCollection services)
    {
        var existing = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAuthorizationMiddlewareResultHandler));

        services.AddSingleton<IAuthorizationMiddlewareResultHandler>(provider =>
        {
            var inner = existing?.ImplementationInstance as IAuthorizationMiddlewareResultHandler
                ?? existing?.ImplementationFactory?.Invoke(provider) as IAuthorizationMiddlewareResultHandler
                ?? (existing?.ImplementationType is null
                    ? new AuthorizationMiddlewareResultHandler()
                    : (IAuthorizationMiddlewareResultHandler)ActivatorUtilities.CreateInstance(provider, existing.ImplementationType));

            return new SiblingWriter(inner);
        });
    }

    /// <summary>
    /// Never authenticates, so the framework handler has a scheme to forbid through.
    /// </summary>
    private sealed class StubAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public StubAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }
    }

    /// <summary>
    /// A generic body writer using the same guard the sibling package uses.
    /// </summary>
    private sealed class SiblingWriter : IAuthorizationMiddlewareResultHandler
    {
        private readonly IAuthorizationMiddlewareResultHandler inner;

        public SiblingWriter(IAuthorizationMiddlewareResultHandler inner)
        {
            this.inner = inner;
        }

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            await this.inner.HandleAsync(next, context, policy, authorizeResult);

            if (authorizeResult.Succeeded || context.Response.HasStarted)
            {
                return;
            }

            if (context.Response.ContentLength is > 0 || context.Response.ContentType is not null)
            {
                return;
            }

            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync("{\"status\":403,\"errors\":[{\"code\":\"GENERIC\"}]}");
        }
    }
}

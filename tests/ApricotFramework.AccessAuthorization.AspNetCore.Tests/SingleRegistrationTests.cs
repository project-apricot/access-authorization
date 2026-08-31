using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.AspNetCore.Scopes.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.AspNetCore.Tests;

public class SingleRegistrationTests
{
    private static async Task<int> InvokeAsync(params object[] metadata)
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAuthentication(TestAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, null);
        services.AddAuthorization();
        services.AddAccessAuthorization();

        using var provider = services.BuildServiceProvider(validateScopes: true);
        using var scope = provider.CreateScope();

        var reached = false;
        var middleware = new AuthorizationMiddleware(
            _ => { reached = true; return Task.CompletedTask; },
            scope.ServiceProvider.GetRequiredService<IAuthorizationPolicyProvider>());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = ClaimsPrincipalOrNone.User(("scope", "sales.read")).Principal,
        };

        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(metadata), "test"));

        await middleware.Invoke(httpContext);

        return reached ? 200 : httpContext.Response.StatusCode;
    }

    [Fact]
    public async Task ScopeAttribute_WithOnlyTheOneRegistrationCall_AdmitsAValidCaller()
    {
        // This failed with 403 while scope authorization needed a registration call of its own: the
        // attribute contributed a requirement, no handler existed to satisfy it, and nothing warned.
        Assert.Equal(200, await InvokeAsync(new AuthorizeAllScopesAttribute("sales.read")));
    }

    [Fact]
    public async Task AccessAndScopeAttributes_WithOnlyTheOneRegistrationCall_BothTakeEffect()
    {
        var scopeSatisfied = await InvokeAsync(new AuthorizeAllScopesAttribute("sales.read"));
        var scopeMissing = await InvokeAsync(new AuthorizeAllScopesAttribute("sales.write"));

        Assert.Equal(200, scopeSatisfied);
        Assert.Equal(403, scopeMissing);
    }
}

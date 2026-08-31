using System.Text.Json;
using ApricotFramework.AccessAuthorization.ErrorDefinitions.Extensions;
using ApricotFramework.AccessAuthorization.Extensions;
using ApricotFramework.AccessAuthorization.Scopes;
using ApricotFramework.ErrorDefinitions;
using ApricotFramework.ErrorDefinitions.AspNetCore;
using ApricotFramework.ErrorDefinitions.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ApricotFramework.AccessAuthorization.ErrorDefinitions.Tests;

public class AuthorizationExceptionMapperTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorizationErrorDefinitions();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static IReadOnlyList<ErrorDefinition>? Map(ServiceProvider provider, Exception exception)
    {
        var mapper = Assert.Single(provider.GetServices<IExceptionErrorMapper>(), candidate => candidate.GetType().Name == "AuthorizationExceptionMapper");

        return mapper.Map(new DefaultHttpContext(), exception);
    }

    [Fact]
    public void Map_AccessFailure_IsAccessDeniedWithItsOwnCode()
    {
        using var provider = BuildProvider();

        var errors = Map(provider, new AccessAuthorizationException("nope", ["sales:orders:edit"]));

        var error = Assert.Single(errors!);

        Assert.Equal(ErrorKinds.AccessDenied, error.Kind);
        Assert.Equal(AccessAuthorizationErrors.AccessNotGranted, error.Code);
        Assert.Equal("nope", error.Message);
    }

    [Fact]
    public void Map_AccessFailure_NamesWhatWasMissing()
    {
        using var provider = BuildProvider();

        var errors = Map(provider, new AccessAuthorizationException("nope", ["sales:orders:edit"]));

        var missing = Assert.IsAssignableFrom<IEnumerable<string>>(Assert.Single(errors!).Payload!["missing"]);

        Assert.Equal(["sales:orders:edit"], missing);
    }

    [Fact]
    public void Map_ScopeFailure_IsDistinguishableFromAnAccessFailure()
    {
        using var provider = BuildProvider();

        var errors = Map(provider, new ScopeAuthorizationException("nope", ["billing.write"]));

        Assert.Equal(AccessAuthorizationErrors.ScopeNotGranted, Assert.Single(errors!).Code);
    }

    [Fact]
    public void Map_FailureWithNothingNamed_HasNoPayload()
    {
        using var provider = BuildProvider();

        var errors = Map(provider, new AccessAuthorizationException("nope"));

        Assert.Null(Assert.Single(errors!).Payload);
    }

    [Fact]
    public void Map_UnrelatedException_ReturnsNull()
    {
        // Answering for an exception this library does not own would silence every mapper after it.
        using var provider = BuildProvider();

        Assert.Null(Map(provider, new InvalidOperationException("unrelated")));
    }

    [Fact]
    public void Map_TheBaseAuthorizationException_ReturnsNull()
    {
        using var provider = BuildProvider();

        Assert.Null(Map(provider, new AuthorizationException("unclassified")));
    }

    [Fact]
    public void AddAccessAuthorizationErrorDefinitions_Always_BringsInTheSharedHandler()
    {
        // One handler answers for every library, rather than each registering its own and competing.
        using var provider = BuildProvider();

        Assert.NotEmpty(provider.GetServices<Microsoft.AspNetCore.Diagnostics.IExceptionHandler>());
    }

    [Fact]
    public void AddAccessAuthorizationErrorDefinitions_CalledTwice_RegistersOneMapper()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddAccessAuthorizationErrorDefinitions();
        services.AddAccessAuthorizationErrorDefinitions();

        using var provider = services.BuildServiceProvider(validateScopes: true);

        Assert.Single(provider.GetServices<IExceptionErrorMapper>(), candidate => candidate.GetType().Name == "AuthorizationExceptionMapper");
    }

    [Fact]
    public void AddAccessAuthorizationErrorDefinitions_Always_AlsoCoversPolicyDenials()
    {
        // The mapper covers exceptions; a policy denial raises none, so the middleware result handler
        // is decorated as well. See AccessAuthorizationResultWriterTests.
        using var provider = BuildProvider();

        Assert.NotNull(provider.GetService<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler>());
    }

    [Fact]
    public async Task WriteErrorProblemDetailsAsync_MappedFailure_SerialisesTheMissingAccesses()
    {
        using var provider = BuildProvider();

        var errors = Map(provider, new AccessAuthorizationException("nope", ["sales:orders:edit"]))!;

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        using var body = new MemoryStream();

        httpContext.Response.Body = body;

        await httpContext.WriteErrorProblemDetailsAsync(errors, cancellationToken: TestContext.Current.CancellationToken);

        var payload = JsonDocument.Parse(body.ToArray());
        var first = payload.RootElement.GetProperty("errors")[0];

        Assert.Equal(403, httpContext.Response.StatusCode);
        Assert.Equal(AccessAuthorizationErrors.AccessNotGranted, first.GetProperty("code").GetString());
        Assert.Equal("sales:orders:edit", first.GetProperty("payload").GetProperty("missing")[0].GetString());
    }

    [Fact]
    public void AddAccessAuthorizationErrorDefinitions_NullServices_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddAccessAuthorizationErrorDefinitions());
    }
}

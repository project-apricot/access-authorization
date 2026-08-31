using ApricotFramework.AccessAuthorization;
using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;
using ApricotFramework.AccessAuthorization.ErrorDefinitions.Extensions;
using ApricotFramework.AccessAuthorization.Examples.Web.Access;
using ApricotFramework.AccessAuthorization.Examples.Web.Grpc;
using ApricotFramework.AccessAuthorization.Examples.Web.Model;
using ApricotFramework.AccessAuthorization.Extensions;
using Microsoft.AspNetCore.Authentication;
using DemoAuth = ApricotFramework.AccessAuthorization.Examples.Web.Auth.DemoAuthenticationHandler;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(DemoAuth.SchemeName).AddScheme<AuthenticationSchemeOptions, DemoAuth>(DemoAuth.SchemeName, null);

builder.Services.AddAuthorization();

// One call covers both accesses and token scopes.
builder.Services.AddAccessAuthorization(builder.Configuration);

// Where assigned accesses come from. The library never learns the shape of the store.
builder.Services.AddAccessStore<InMemoryAccessStore>();

// Declared so "what may this caller do?" has a candidate set to answer from.
builder.Services.AddAccessCatalog(OrderAccesses.All());

// Order is by stage, not by the order these two lines appear in.
builder.Services.AddAccessBypassRule<RootBypassRule>();
builder.Services.AddAccessRule<OrderAttributeRule>();

builder.Services.AddAccessAuthorizationErrorDefinitions();
builder.Services.AddControllers();
builder.Services.AddGrpc();

var app = builder.Build();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<OrdersGrpcService>();

// A minimal-API endpoint gated the same way an attribute would gate a controller action.
app.MapGet("/minimal/orders/{id}", (string id) => new { Id = id, Source = "minimal-api" })
    .RequireAccessAny(OrderAccesses.Read);

// The service-to-service shape: a scope, and no subject anywhere in sight.
app.MapGet("/minimal/report", () => new { Report = "ok" })
    .RequireScopesAll("sales.read");

// What the application declares, before any subject is considered. Useful for an admin screen that
// assigns accesses, or for documenting the surface.
app.MapGet("/declared", (IAccessCatalog catalog, string? resourceType) =>
{
    var definitions = catalog.GetDefinitions();

    return Results.Ok(new
    {
        All = definitions.Select(definition => new { definition.Access, definition.ResourceType }).OrderBy(entry => entry.Access, StringComparer.Ordinal),
        ForResourceType = resourceType is null ? null : catalog.GetCandidates(AccessResource.OfType(resourceType)).Order(StringComparer.Ordinal),
    });
}).AllowAnonymous();

// What a user interface calls to decide which buttons to render.
app.MapGet("/capabilities", async (HttpContext httpContext, string? orderId, CancellationToken cancellationToken) =>
{
    var resource = orderId is null ? null : OrderRepository.Describe(orderId);
    var evaluation = await httpContext.GetAllowedAccessesAsync(resource, cancellationToken);

    return Results.Ok(new { OrderId = orderId, Allowed = evaluation.Allowed.Order(StringComparer.Ordinal) });
});

// Walks the whole model in one response, for whoever is reading the example rather than running it.
app.MapGet("/demo", async (HttpContext httpContext, IAccessAuthorization authorization, CancellationToken cancellationToken) =>
{
    var subject = httpContext.GetAccessSubject();

    if (subject is null)
    {
        return Results.Ok(new { Subject = (string?)null, Note = "No subject on this request, so every access is denied. Send X-Demo-Subject." });
    }

    var owned = OrderRepository.Describe("42");
    var foreignOrder = OrderRepository.Describe("43");

    return Results.Ok(new
    {
        Subject = subject.Id,
        Attributes = subject.Attributes,
        AssignedOnly = (await authorization.GetAllowedAsync(AccessContext.For(subject), OrderAccesses.All().Select(definition => definition.Access), cancellationToken)).Allowed.Order(StringComparer.Ordinal),
        OnOrder42 = (await httpContext.GetAllowedAccessesAsync(owned, cancellationToken)).Allowed.Order(StringComparer.Ordinal),
        OnOrder43 = (await httpContext.GetAllowedAccessesAsync(foreignOrder, cancellationToken)).Allowed.Order(StringComparer.Ordinal),
        Note = "OnOrder42 gains edit for subject 'reader' because the owner rule grants it, which no assigned access could express.",
    });
});

app.Run();

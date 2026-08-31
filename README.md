# ApricotFramework.AccessAuthorization

[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.AccessAuthorization.svg?label=ApricotFramework.AccessAuthorization)](https://www.nuget.org/packages/ApricotFramework.AccessAuthorization/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.AccessAuthorization.AspNetCore.svg?label=ApricotFramework.AccessAuthorization.AspNetCore)](https://www.nuget.org/packages/ApricotFramework.AccessAuthorization.AspNetCore/)
[![NuGet](https://img.shields.io/nuget/v/ApricotFramework.AccessAuthorization.ErrorDefinitions.svg?label=ApricotFramework.AccessAuthorization.ErrorDefinitions)](https://www.nuget.org/packages/ApricotFramework.AccessAuthorization.ErrorDefinitions/)
[![CI](https://github.com/project-apricot/access-authorization/actions/workflows/ci.yml/badge.svg)](https://github.com/project-apricot/access-authorization/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](https://github.com/project-apricot/access-authorization/blob/main/LICENSE)

Authorization for .NET built on an ordered rule pipeline: deny by default, with statically assigned
accesses as one rule among several. One call answers both *is this allowed* and *what is allowed*, so
a user interface and a gate can never disagree.

`ApricotFramework.AccessAuthorization` is the **zero-dependency** core.

## Install

```bash
dotnet add package ApricotFramework.AccessAuthorization
dotnet add package ApricotFramework.AccessAuthorization.AspNetCore
```

## Usage

```csharp
using ApricotFramework.AccessAuthorization.AspNetCore.Extensions;

builder.Services.AddAccessAuthorization(builder.Configuration);
builder.Services.AddAccessStore<MyAccessStore>();        // where assigned accesses come from
builder.Services.AddAccessCatalog(ContentAccesses.All);  // needed only to list capabilities
builder.Services.AddAccessBypassRule<RootBypassRule>();  // runs before the assigned lookup
builder.Services.AddAccessRule<OwnerCanEditRule>();      // runs after it
```

The attributes work unchanged on a controller action, a minimal-API endpoint and a **gRPC method**,
because they carry their requirement as endpoint metadata rather than running as an MVC filter:

```csharp
[AuthorizeAnyAccess(ContentAccesses.OrdersRead)]
public object Get(string id) => this.repository.Find(id);

[AuthorizeAllScopes("sales.read")]                        // on a gRPC method
public override Task<GetOrderReply> GetOrder(GetOrderRequest request, ServerCallContext context) => …
```

And the listing a user interface renders from, which runs the same pipeline as the gates:

```csharp
var allowed = await httpContext.GetAllowedAccessesAsync(AccessResource.Create("sales:order", id));
```

Subject attributes are strings, since they come from claims and key the cache; resource and
environment attributes keep their domain types, so a rule matches rather than parses
(`if (resource.Attributes["isPublic"] is true)`).

Note that **an assigned access ignores the resource**: a granted `sales:orders:edit` means the
subject may edit orders, not one particular order. Narrowing a grant to an instance is a rule of your
own, registered after the assigned lookup — see the docs for why that division is deliberate.

Denials carry an RFC 9457 problem-details body — from an attribute as well as from an imperative
check — if you add `ApricotFramework.AccessAuthorization.ErrorDefinitions`.

Full documentation: <https://projectapricot.dev/docs/access-authorization>

# Zongsoft.Web Web Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Web) is the _**W**eb_ library in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides functionality for [_ASP.NET_](https://learn.microsoft.com/aspnet) application development.

It is the framework's ASP.NET Core integration layer: service-oriented controllers, model binding and JSON formatting, credential authentication, controller metadata, file endpoints, SignalR discovery, and multi-site configuration. Add [Zongsoft.Plugins.Web](../Zongsoft.Plugins.Web/README.md) when the application is assembled from plugin manifests.

## Concepts

- A **service controller** projects an `IDataService<TModel>` as HTTP operations while retaining filtering, sorting, paging, validation, and authorization semantics.
- A **controller descriptor** gives tooling a stable view of modules, services, routes, operations, and parameters derived from MVC's application model.
- A **binder** converts compact HTTP values such as ranges, mixtures, booleans, paging, and sorting into framework abstractions.
- A **site** describes a logical web site and its hosts; it is configuration metadata, not another ASP.NET server.

See the [ASP.NET Core MVC overview](https://learn.microsoft.com/aspnet/core/mvc/overview) for the underlying request model.

## Installation

```shell
dotnet add package Zongsoft.Web
```

## Real Controller: Discussions Forums

[ForumController](../../discussions/src/api/Controllers/ForumController.cs) exposes the forum domain service over HTTP. This excerpt contains its declaration and moderator-query action, not invented models or the complete source file:

```csharp
[ControllerName("Forums")]
public class ForumController : ServiceController<Forum, ForumService>
{
	[ActionName("Moderators")]
	[HttpGet("{id}/[action]")]
	public IEnumerable<UserProfile> GetModerators(ushort id)
	{
		return this.DataService.GetModerators(id, this.Request.Headers.GetDataSchema());
	}
}
```

Models come from [Discussions Models](../../discussions/src/Models/) and the service from [ForumService](../../discussions/src/Services/ForumService.cs). The base provides standard operations according to service capabilities; the derived controller adds forum-specific actions. `GetDataSchema()` passes the request schema to the service.

Deploy the complete domain and Web plugins and configure data, security and site dependencies before calling these actions; do not construct drivers in controllers. See the [real Plugins.Web use case](../Zongsoft.Plugins.Web/README.md) for composition and request templates.

💡 Final routes combine module ownership, `ControllerName`, the base `[area]/[controller]` route and action templates. Use source, existing [.http requests](../../discussions/docs/http/forum.http) and host descriptors instead of inventing a product API.

## HTTP Conventions

`WebUtility.Paginate` writes paging metadata to response headers. The JSON formatters use the framework serializer. Binders understand `Paging`, `Sorting`, `Range<T>`, `Mixture<T>`, booleans, and time spans; invalid input is reported through MVC model state.

Register the credential scheme through ASP.NET authentication:

```csharp
builder.Services
	.AddAuthentication()
	.AddCredentials();
```

`AuthorizationAttribute` and `AuthorizationConvention` connect operations to framework authorization metadata. Credential issuance and privilege services live in [Zongsoft.Security](../Zongsoft.Security/README.md).

🚨 Authentication identifies a caller; it does not authorize every endpoint. Configure policies explicitly, and never log raw credentials or place them in query strings.

## Files, SignalR, and Extension Points

`WebFileAccessor` and the built-in file/directory controllers expose framework file systems over HTTP; constrain roots, permissions, sizes, and media types before enabling them. SignalR discovery identifies Hub implementations for plugin-aware hosts. Host and site collections provide matching metadata through `IWebEnvironment`.

Extension points include the generic service controllers, MVC conventions, binders, formatters, filters, credential options, web host/site abstractions, and `ControllerServiceDescriptor` metadata.

## Related Resources

- [Plugin-aware web hosting](../Zongsoft.Plugins.Web/README.md)
- [OpenAPI integration](openapi/README.md)
- [gRPC integration](grpc/README.md)
- [Implementation guidance](SKILL.md)

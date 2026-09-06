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

## Minimal Controller

```csharp
using Microsoft.AspNetCore.Mvc;
using Zongsoft.Data;
using Zongsoft.Web;

[ApiController]
[Route("api/products")]
public sealed class ProductController(IDataService<Product> service) :
	ServiceController<Product, IDataService<Product>>
{
	protected override IDataService<Product> GetService() => service;
}

public sealed class Product
{
	public int ProductId { get; set; }
	public string Name { get; set; } = string.Empty;
}
```

The base exposes count, existence, query, create, update, upsert, and delete workflows according to the data service's capabilities. Override protected hooks or disable operations instead of duplicating the action set.

The controller above consumes an existing data service: the application must register its `IDataService<Product>` implementation, entity mapping and connection. Copying the controller alone does not create a working database. In a plugin application, place it in the application's own assembly and manifest for host discovery and container injection; do not construct a data engine or driver inside the controller. See the [complete Plugins.Web example](../Zongsoft.Plugins.Web/README.md) for a runnable HTTP workflow without a database.

> 💡 Controller conventions and attributes affect final routes. Generate OpenAPI or inspect controller descriptors rather than constructing client URLs from class names.

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

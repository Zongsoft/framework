# Zongsoft.Web.OpenApi OpenAPI Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Web.OpenApi)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Web.OpenApi)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

---

## Overview

[**Z**ongsoft.**W**eb.**O**pen**A**pi](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Web/openapi) is the [***O**pen **API***](https://www.openapis.org) plugin library in the [***Z**ongsoft*](https://github.com/Zongsoft/framework) open-source framework. It provides out-of-the-box [***O**pen **API***](https://www.openapis.org) features for [*ASP.NET*](https://learn.microsoft.com/aspnet) applications.

The library automatically discovers the controllers and operations of the current application, generates an **OpenAPI 3.1** document (in `JSON` or `YAML`), and serves it through a dedicated endpoint with an integrated [**Scalar**](https://github.com/scalar/scalar) interactive UI — no business code needs to be modified.

## Features

- **Zero-code integration**: deployed as a Zongsoft plugin; the host registers the document endpoint and the Scalar UI automatically.
- **Automatic discovery**: all controllers and operations of the application are collected into a `ControllerServiceDescriptorCollection` and documented.
- **Attribute routing**: paths, HTTP methods, parameters (path/query/header/body) and schemas are generated from `ControllerModel`/`ActionModel`.
- **Conventional routing support**: interfaces exposed via conventional routing (e.g. `MapControllerRoute("default", "{controller}/{action}/{id?}")`) are resolved from the runtime route table (`EndpointDataSource`) instead of being skipped.
- **Security schemes**: declare authentication schemes (e.g. `Credential`, `Bearer`, `Basic`, `OAuth2`, `OpenID`) through configuration.
- **Servers & environments**: define server URLs and environments with templated variables.
- **Common headers**: inject common request headers into every operation via `IHeaderProvider`.
- **Scalar UI**: built-in interactive API explorer (via `Scalar.AspNetCore`).
- **Plugin-style deployment**: ship as `.plugin`/`.option` artifacts and enable/disable it per host.

## Getting Started

### 1. Deploy the plugin

Copy the `Zongsoft.Web.OpenApi` plugin into the `plugins` directory of your Zongsoft host application, e.g. `plugins/zongsoft/openapi/`:

```
plugins/
└── zongsoft/
    └── openapi/
        ├── Zongsoft.Web.OpenApi.plugin
        ├── Zongsoft.Web.OpenApi.option
        ├── Zongsoft.Web.OpenApi.dll
        └── Microsoft.OpenApi.dll
```

The `WebInitializer` of the plugin automatically calls `app.UseOpenApi()` and `app.MapScalarApiReference()` when the host starts, so **no code changes are required** in your application.

### 2. Access the document

After the host is started, the following endpoints are available:

| Endpoint           | Description                                    |
| ------------------ | ---------------------------------------------- |
| `/openapi/v1.yaml` | OpenAPI document in YAML                       |
| `/openapi/v1.json` | OpenAPI document in JSON                       |
| `/openapi/v1.yml`  | alias of the YAML format                       |
| `/scalar`          | interactive Scalar UI (Swagger-style explorer) |

The route pattern is `/openapi/{documentName}.{extension}`, and the default document name is `v1`. If you need a different route, register the endpoint manually:

```csharp
app.UseOpenApi("/docs/{documentName}.{extension}");
```

## How It Works

### Document generation

`DocumentGenerator.Generate(context)` builds an `OpenApiDocument` from the `ControllerServiceDescriptorCollection` stored in `ApplicationContext.Current.Properties`:

- `GenerateEnvironments` — environments list (`x-environment` extension, Scalar active environment, etc.);
- `GenerateServers` — server list with templated variables;
- `GenerateSecuritySchemes` — security schemes from the `<authentication>` configuration;
- `GeneratePaths` — paths, operations and parameters;
- `GenerateSchemas` — component schemas referenced by the operations.

### Route resolution

1. **Attribute routing**: paths are resolved from the route templates of the controller/action selectors (`AttributeRouteModel`/`IRouteTemplateProvider`). Templates that are empty or `null` (e.g. a bare `[HttpGet]` without a template) are ignored, because they express an HTTP method predicate only, not a route template.
2. **Conventional routing**: for operations that have no attribute route templates, the generator queries the runtime route table (`EndpointDataSource` resolved from the request services) and matches the corresponding `RouteEndpoint` by `ControllerActionDescriptor` (controller/action names) and `HttpMethodMetadata`. The template text (`RoutePattern.RawText`, e.g. `/quartz/{controller}/{action}`) is then instantiated with the actual route values from `ControllerActionDescriptor.RouteValues` (e.g. `/quartz/Jobs/Edit`). This guarantees that interfaces such as the SilkierQuartz dashboard are documented with their real URLs instead of being skipped.

### Parameter inference

Parameters without an explicit binding source (`[From*]`) are inferred as follows:

- the parameter name exists in the route template → `in: path`;
- the parameter type is a scalar type → `in: query`;
- otherwise it is treated as a body parameter (`requestBody` with an `application/json` schema).

## Configuration

All options are configured under the `/web/openapi` configuration path (see `Zongsoft.Web.OpenApi.option` for the default values):

```xml
<options>
	<option path="/web/openapi">
		<authentication persisted="true">
			<authenticator authenticator.name="credential" kind="http" scheme="Credential" />
			<authenticator authenticator.name="bearer" kind="http" scheme="Bearer" />
			<authenticator authenticator.name="basic" kind="http" scheme="Basic">
				<username>{{username}}</username>
				<password>{{password}}</password>
			</authenticator>
		</authentication>

		<headers>
			<header header.name="X-Json-Behaviors" method="GET,POST" value="ignores:null,empty;casing:camel" />
		</headers>

		<servers>
			<server server.name="ports" url="http://{host}:{port}">
				<variable variable.name="host" default="localhost">
					<value>localhost</value>
					<value>127.0.0.1</value>
				</variable>
			</server>
		</servers>

		<environments>
			<environment environment.name="development">
				<variable variable.name="host" default="192.168.0.101" />
			</environment>
		</environments>
	</option>
</options>
```

### Authentication

- `persisted` — whether the selected scheme should be persisted in the Scalar UI;
- `authenticator` — declares a security scheme:
  - `name` — scheme identifier;
  - `kind` — `http` | `custom` | `oauth2` | `openid`;
  - `scheme` — HTTP authentication scheme name (for `kind="http"`);
  - `location` — `header` | `cookie` | `query` | `path` (for `kind="custom"`);
  - `username`/`password` — credentials for `Basic` authentication.

### Headers

Each `<header>` declares a common request header (`name`/`value`) applied to the given HTTP methods (`method`, comma-separated). The built-in `HeaderProvider` implements `IHeaderProvider`; you can replace or add providers via the service container to customize header injection.

### Servers & environments

Both servers and environments support templated `url` values with `<variable>` placeholders (`{name}`). Each variable declares its `default` value and an optional list of `<value>` candidates, which the Scalar UI renders as dropdown selections.

## Extension Points

| Interface                  | Purpose                                                       |
| -------------------------- | ------------------------------------------------------------- |
| `Services.IHeaderProvider` | Supplies common request headers per operation and HTTP method |

## Directory Layout

```
openapi/
├── DocumentGenerator.cs            # document generation entry
├── DocumentGenerator.*.cs          # environments / servers / security / paths / schemas
├── DocumentContext.cs              # generation context (format, services, endpoints, ...)
├── DocumentFormat.cs               # json / yaml format
├── Tags.cs                         # tag generation (x-parent, x-displayName, ...)
├── Utility.cs                      # parameter location & type utilities
├── WebExtension.cs                 # UseOpenApi() endpoint extension
├── WebExtension.Writer.cs          # UTF-8 buffered writer for the response
├── WebInitializer.cs               # automatic registration of the endpoint & Scalar UI
├── Configuration/                  # authentication / header / server / environment / variable options
├── Services/                       # IHeaderProvider and its implementation
└── Extensions/                     # OpenAPI extension (x-*) builders
```

## License

The **Zongsoft.Web.OpenApi** library is released under the [GNU Lesser General Public License v3.0](../../LICENSE).

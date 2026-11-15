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

The plugin discovers the application's controllers and operations and serves **OpenAPI 3.2.1** in JSON and YAML. The built-in [Scalar](https://github.com/scalar/scalar) interface uses the document to display a nested API directory. The plugin targets `net10.0` and `net11.0`.

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
| `/scalar/`         | interactive Scalar UI with manual mode and configured environments |

The route pattern is `/openapi/{documentName}.{extension}`, and the default document name is `v1`. If you need a different route, register the endpoint manually:

```csharp
app.UseOpenApi("/docs/{documentName}.{extension}");
```

## API directory

The directory follows the application module, each level of the service namespace, and the service. Nested controllers provide nested service namespaces. Scalar displays these levels as expandable sections. A service can contain its own operations and child sections.

Automation can follow `tags[].parent` to navigate the directory and match operation tags against `tags[].name`. `tags[].summary` is the display title. An operation's tag name is the service's qualified name. Both document formats contain the same directory and operations.

Operations with multiple documented routes or methods get unique `operationId` values. The first entry in ordinal path and HTTP-method order retains the original identifier. The other entries include the method and escaped path. Response status codes and payloads require an explicit contract. Generated operations without response metadata carry a generic `default` response.

### Host compatibility

The plugin uses `Microsoft.OpenApi` 3.x on both supported target frameworks. `Microsoft.AspNetCore.OpenApi` 10.x requires `Microsoft.OpenApi` 2.x. A .NET 10 application that requires that generator can serve the two documents from different hosts. [OpenAPI.NET migration guide](https://github.com/microsoft/OpenAPI.NET/blob/main/docs/upgrade-guide-3.md#integrations-with-aspnet-core)

## Routes and parameters

### Route resolution

Path keys under `paths` start with `/` in both JSON and YAML.

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
			<server server.name="current" />
			<server server.name="ports" description="Host and port" url="{scheme}://{host}:{port}">
				<variable variable.name="scheme" default="http">
					<value>http</value>
					<value>https</value>
				</variable>
				<variable variable.name="host" default="localhost" />
				<variable variable.name="port" default="8069" />
			</server>
			<server server.name="sites" description="Site, host and port" url="{scheme}://{site}.{host}:{port}">
				<variable variable.name="scheme" default="http">
					<value>http</value>
					<value>https</value>
				</variable>
				<variable variable.name="site" default="api.b">
					<value>api.a</value>
					<value>api.b</value>
					<value>api.c</value>
					<value>api.iot</value>
					<value>api.gateway</value>
				</variable>
				<variable variable.name="host" default="zongsoft.com" />
				<variable variable.name="port" default="80" />
			</server>
		</servers>

		<environments>
			<environment environment.name="development">
				<variable variable.name="username" default="" />
				<variable variable.name="password" default="" />
			</environment>
		</environments>
	</option>
</options>
```

### Authentication

Set `authentication persisted="true"` to retain authentication settings in browser storage. The shipped options enable this; without the setting, persistence is disabled. Enter valid credentials in Scalar before calling protected endpoints. The plugin does not prefill a generated credential.

To reuse a Zongsoft login credential, run the sign-in API, then open **Test Request → Auth**, select `credential`, and enter `Credential <id>` in **Value**. Scalar does not extract the ID from the sign-in response. With persistence enabled, Scalar remembers the value per document across reloads. If you change the server address within a document, check that the saved credential still belongs to the target.

- `authenticator` — declares a security scheme. The authentication selector follows XML declaration order:
  - `name` — scheme identifier;
  - `kind` — `http` | `custom` | `oauth2` | `openid`;
  - `scheme` — HTTP authentication scheme name (for `kind="http"`);
  - `location` — `header` | `cookie` | `query` | `path` (for `kind="custom"`);
  - `username`/`password` — credentials for `Basic` authentication.

### Headers

Each `<header>` declares a common request header (`name`/`value`) applied to the given HTTP methods (`method`, comma-separated). The built-in `HeaderProvider` implements `IHeaderProvider`; you can replace or add providers via the service container to customize header injection.

### Servers & environments

The first server is always `current`. Without an explicit URL it uses the document's origin and the request's `PathBase`, so the actual scheme, host and port are used automatically. A root deployment emits `url: /`; an application mounted at `/gateway/app` emits `url: /gateway/app`. Selecting **Current site** therefore requires no port configuration.

The server selector offers **current**, **ports** and **sites**. **Host and port** uses `{scheme}://{host}:{port}` and **Site, host and port** uses `{scheme}://{site}.{host}:{port}`.

In **sites**, choose a subdomain from the `site` dropdown. It offers `api.a`, `api.b`, `api.c`, `api.iot` and `api.gateway`, with `api.b` selected by default. To change the choices, edit the `<value>` entries on `site`. Enter the host and port in their text fields. Use **ports** for a plain IP address and wrap an IPv6 host in brackets, as in `[::1]`.

A complete-address input is optional and is not included in the default selector. To paste a complete URL, including a base path, add this server:

```xml
<server server.name="full-url" description="Complete service URL" url="{url}">
	<variable variable.name="url" default="http://localhost:8069" />
</server>
```

Each server's `server.name` is its stable OpenAPI `name`; `description` provides explanatory text and falls back to the name. Scalar may show the server name or URL in its selector. The reserved name `current` is case-insensitive and remains first. Other servers follow XML declaration order. You can explicitly set the current site's public base URL when the proxy cannot populate `Request.PathBase`:

```xml
<server server.name="current" description="Current site" url="/gateway/app" />
<server server.name="staging" description="Staging" url="https://staging.example.com/api" />
```

Server URLs support `{name}` placeholders. A variable has a `default` and an optional `description`. A nonempty `<value>` list defines the dropdown choices in XML declaration order. The default must exactly match one of the choices. Omit the list for a free text field. Variables appear in the order of their first occurrence in the URL template.

Configure proxy forwarding and the application's base path in the host before exposing the document endpoints. Relative server URLs follow the document origin rather than an internal listen address. Downloaded documents need an explicit base URL when used by an offline client or automated tests. Cross-origin browser requests require the target server's CORS policy to allow the documentation origin; HTTPS pages also require HTTPS API targets.

Open `/scalar/` to use Scalar. It starts in **none**, which offers the manual server selector. To add a fixed deployment target, set `url` on an environment under `/web/openapi`.

```xml
<environments>
	<environment environment.name="development-a" url="http://localhost:5801">
		<variable variable.name="site" default="a" />
	</environment>
	<environment environment.name="development-b" url="http://localhost:5802/api">
		<variable variable.name="site" default="b" />
	</environment>
</environments>
```

The document selector at the top left of `/scalar/` lists **none** first, followed by the environments with a configured `url` in XML declaration order. The environment name appears in the dropdown, the browser fragment, and the document URL, such as `/scalar/#development-a` and `/openapi/environments/development-a.json`. The optional `description` describes the server. Environments without a URL still provide variables to the manual document. When switching environments from an operation, Scalar can keep the operation suffix in the fragment. Open the environment's short URL, for example `/scalar/#staging-a`, to go directly to its document.

Each environment has its own `<variable>` entries. For example, reference `{{site}}` in a request field to use the current environment's site value. The `url` still provides the full server address.

Environment names use lowercase ASCII letters and digits, with single hyphens between groups, up to 255 characters. The name `none` is reserved for manual mode. The URL must be an absolute HTTP or HTTPS address without user information, query string or fragment. Include the port and base path if needed. For browser requests to a different origin, configure that environment to allow the documentation origin through CORS. Credentials are entered through the client. Keep actual passwords and tokens out of the public configuration and documents.

## Extension Points

| Interface                  | Purpose                                                       |
| -------------------------- | ------------------------------------------------------------- |
| `Services.IHeaderProvider` | Supplies common request headers per operation and HTTP method |

## License

The **Zongsoft.Web.OpenApi** library is released under the [MIT License](https://github.com/Zongsoft/framework/blob/main/LICENSE).

## Plugin-Based Integration

The host supplies Zongsoft.Core, Zongsoft.Web, Zongsoft.Plugins and Zongsoft.Plugins.Web. Deployment manifests never reference these four libraries; compile-time dependencies remain in the project file.

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Use a Plugins.Web host. Its initializer discovers this package's service registration and maps OpenAPI/Scalar according to the deployed options; review exposure and authentication before publishing documentation endpoints.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Web.OpenApi` | [Zongsoft.Web.OpenApi.plugin](src/Zongsoft.Web.OpenApi.plugin) |
| File copying and dependencies | [Zongsoft.Web.OpenApi.deploy](src/Zongsoft.Web.OpenApi.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft web openapi]
nuget:Zongsoft.Web.OpenApi
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Web.OpenApi.option`, `Zongsoft.Web.OpenApi.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

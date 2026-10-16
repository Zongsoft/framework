# Web endpoints and request scopes

[English](endpoints.md) | [简体中文](endpoints.zh-Hans.md)

`Zongsoft.Web.Hosting` provides independent Kestrel configuration conversion, server-independent logical endpoints and request-scope constraints. Standalone ASP.NET hosts must explicitly connect these facilities after configuration loading. Referencing the assembly or adding a zongsoft.tools.binding processing instruction does not install the pipeline. Plugins.Web uses IServiceRegistration scanning to automatically connect conversion, scopes and monitoring when a Web host declares Kestrel:Endpoints and has no explicit endpoint registration; hosting code needs no changes. gRPC service registration belongs to Web.Grpc.

## Host integration

Place this ordinary HTTP example after an existing ASP.NET Core WebApplicationBuilder has loaded its final configuration. Keep projection alive until the application has stopped and been disposed.

```csharp
using Zongsoft.Web.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using var projection = KestrelUtility.Convert(
	builder.Configuration.GetSection("Kestrel"), out var declarations);
var endpoints = builder.Services.AddWebEndpoints(declarations);

builder.WebHost.ConfigureKestrel(options => options.Configure(projection, reloadOnChange: false));
builder.Services.Configure<SocketTransportOptions>(options => KestrelUtility.Configure(options, endpoints));

var app = builder.Build();
app.UseWebEndpoints();
app.UseRouting();
// Add authentication, authorization, static files and existing routes afterwards.
```

`KestrelUtility.Convert(configuration, out declarations, protocols)` produces an independent ConfigurationRoot and read-only logical declarations from the same input. Supply optional normalization rules for protocol shorthand. The caller disposes the result; changes to it cannot mutate the source or logical endpoints. Conversion does not register services, install middleware, subscribe to reloads or start listeners. Empty endpoints produce an independent configuration retaining native settings. The caller selects the section; the utility does not hard-code the Kestrel root path.

`AddWebEndpoints(declarations)` creates request scopes from logical declarations and registered protocol policies. WebEndpoint and WebEndpointCollection can also be constructed directly, without configuration or Kestrel. Apply the projected configuration before original endpoints load. Configuration comparison, restart notifications and projection lifetime belong to the host; the collection has no Configure or HasChanged methods.

Install `UseWebEndpoints` once, before routing, forwarded headers and ordinary HTTP middleware that may return early. It does not replace authentication or authorization. Scope constraints govern listener and route ownership; arbitrary custom middleware remains the application's responsibility. Ordinary requests cannot reach static files or ordinary routes through a gRPC-only scope.

## Snapshot and projection

Each configuration item supplies a complete `Url` and `Protocols`, for example:

```ini
Kestrel:Endpoints:metrics:Url=http://127.0.0.1:4317/opentelemetry
Kestrel:Endpoints:metrics:Protocols=grpc+http2
Kestrel:Endpoints:orders:Url=http://127.0.0.1:4317/orders
Kestrel:Endpoints:orders:Protocols=grpc+http2
```

These logical scopes share one physical HTTP/2 listener. Conversion preserves the source URLs and outputs scheme, host and port for each physical listener. Certificates, SNI and connection defaults are preserved. The converter rejects incompatible settings on shared sockets, overlapping listeners, mixed cleartext HTTP versions and SNI settings that expand declared capabilities. WebEndpointCollection separately rejects conflicting same-kind scopes. Successful conversion does not establish unambiguous request scopes.

`WebEndpoint` accepts a name, NetworkAddress and protocol declarations, which it copies into a read-only collection. Supports(httpVersion) checks a declared HTTP major version. WebEndpointCollection normalizes declarations, builds request-kind scopes and sorts by descending path length and ordinal name. The models and common pipeline do not reference Kestrel types. Conversion uses explicit protocols or EndpointDefaults:Protocols without inferring undeclared capabilities.

Pure conversion accepts explicit carriers such as grpc+http2 and custom+http2 without application-protocol registration. Bare names need a supplied normalization rule. Conversion does not inspect business handlers. Request scopes require the corresponding runtime policies; successful conversion does not imply that those policies are registered.

The Web library supplies ordinary HTTP rules. Register upper-level protocols through `AddWebEndpointProtocol` before creating the snapshot. Valid syntax does not imply an installed runtime policy. Missing protocols and duplicate names or independent kinds fail without ordinary HTTP fallback. The immediate declaration overload rejects later protocol registration; the factory overload permits registration until the final provider is built. Registering the same instance again before snapshot creation is harmless. The optional second argument of `WebEndpointCollection` also accepts policy instances directly. The registration collection is copied; policy instances must remain immutable or thread-safe.

Encoding protocols may share ordinary HTTP. For example, `services.AddWebEndpointProtocol(new WebEndpointProtocol("protobuf"));` declares `protobuf → protobuf+http`; `protobuf,protobuf+http2` normalizes to `protobuf+http2`. This declaration does not install a serializer. Runtime listening requires a specific HTTP version: `http` or the expanded `protobuf+http` alone is insufficient. Direct TCP/UDP combinations are not carried by this Kestrel adapter.

Native gRPC policy belongs to the separate `Zongsoft.Web.Grpc` package. Use `services.AddGrpcWebEndpoints()` before the immediate declaration overload; with the factory overload, complete registration before building the final provider. The plugin's `GrpcInitializer.Registration` registers this policy automatically; Plugins.Web creates snapshots in the final provider through explicit endpoint integration. This policy expands bare `grpc` to `grpc+http2`, accepts HTTP/2 and HTTP/3 carriers, and rejects unsuitable carriers. The Web library contains no gRPC name checks.

An HTTPS `grpc+http3` declaration can produce an HTTP/3 projection. Snapshot creation does not probe the backend or QUIC support. Actual startup remains subject to Kestrel, TLS and platform requirements. HTTP/3 runtime validation is separate from TCP socket validation.

Hosts may be IP addresses, wildcards or localhost; other DNS names are not listener addresses. * explicitly means IPv4. Managed IPv6 TCP sockets set DualMode=false before Bind, while unmanaged sockets retain the original factory. This callback does not configure QUIC UDP sockets. Kestrel expands localhost to IPv4/IPv6 loopback.

## Paths and request kinds

Declared base paths use ASCII letters, digits and -._~ in segments. Empty interior segments, standalone dot segments, escapes, templates and wildcards fail. Base paths ignore case and an optional trailing slash: `/metrics` and `/Metrics/` match themselves and segment children, but not `/metrics-extra`. WebEndpoint.Address retains the declaration; Path removes a non-root trailing slash. Equivalent declarations for the same backend merge; proxy generation rejects equivalent ranges targeting different backends. One logical region may use several listeners.

The pipeline reads actual Connection.LocalIpAddress/LocalPort, connection protocol and IHttpRequestFeature.RawTarget. Host and forwarded headers do not select listeners. Raw paths exclude queries but preserve percent escapes and dot segments. Absolute-form targets retain their original path; OPTIONS * bypasses path mapping. Invalid targets return 400.

Content-Type determines the request kind before longest-prefix selection. Native gRPC includes application/grpc and application/grpc+format, case-insensitively and with media-type parameters. grpc-web is excluded. HTTP/1 gRPC requests return 400; non-POST requests return 405, without ordinary HTTP fallback. An ordinary request without a scope returns 404. A gRPC request without a scope receives an empty trailers-only response with HTTP 200 and grpc-status=12.

After selection, an unsupported version, unknown method or routing failure does not select a shorter scope. If ASP.NET's parsed path enters a different scope or leaves the selected scope, the pipeline returns 404 before ordinary handlers instead of dispatching again. Business parameters may still be decoded within the same scope. This pipeline does not rewrite the body, query, headers, cancellation or original request path.

## Route constraints

`WebEndpointFeature` retains the selected endpoint, raw path and `Kind`. `WebEndpointMetadata` associates an ASP.NET route endpoint with the exact base path and kind:

```csharp
routes.MapGroup("/opentelemetry")
	.WithMetadata(new WebEndpointMetadata("/opentelemetry", "grpc"));
```

The gRPC extension must register services and a regional fallback within that group. Unannotated routes participate only in ordinary HTTP. Annotated routes associate with the selected base path ignoring case and an optional trailing slash. The gRPC adapter checks exact case and encoding only for the native service/method path after that base. A shorter scope's fallback cannot handle unknown methods in a child scope. The Web library adds no gRPC package. Web.Grpc mounts Handler services, regional reflection and regional fallback in these groups; see its [service contract](../grpc/README.md#base-paths-and-regions).

## Protocol extension and responsibilities

`WebEndpointProtocol` describes an upper-level protocol's name, default carrier and request kind through `Name`, `Carrier` and `Kind`. Ordinary encodings can use a direct instance. Independent kinds derive from it, supply `kind` through the protected constructor, and override the steps they need:

| Step | Contract |
| --- | --- |
| Normalize | Complete or validate a declaration without changing its upper-level name; return an empty protocol to reject it |
| Matches | Identify independent kinds before longest-path selection; multiple matches fail, and ordinary HTTP is the default kind |
| Validate | Check requests after classification but before path selection; zero accepts, otherwise return an HTTP status code |
| Accepts | Apply protocol-specific route checks after the common scope checks |
| RejectAsync | Respond when no scope matches; the default is 404 |

The gRPC package's private policy owns media types, method restrictions, exact method paths and grpc-status responses. The Web middleware and matcher policy are private nested classes of `WebEndpointExtension`, not public extension points.

KestrelUtility consumes server configuration. Logical endpoint collections and the common request pipeline consume endpoint declarations and protocol policies. These facilities do not interpret zongsoft.tools.binding processing instructions, generate external entrances or prepend paths to controllers. Ordinary HTTP can likewise use separate `/api` and `/admin` listener scopes. Deployment tools consume binding exposure declarations separately to generate proxy mappings.

## Validation and delivery

[Conversion tests](../test/Hosting/KestrelUtilityTest.cs) cover independent conversion, normalization rules, shared listeners, source isolation and IPv6 TCP binding. [Logical endpoint tests](../test/Hosting/WebEndpointCollectionTest.cs) cover construction without configuration, copied declarations and scope conflicts. [Ordinary pipeline tests](../test/Hosting/WebEndpointPipelineTest.cs) verify independent operation, kinds and registration without referencing the gRPC extension. [gRPC pipeline tests](../grpc/test/GrpcEndpointPipelineTest.cs) cover media types, raw paths, isolation and HTTP/1 plus HTTP/2 requests against an isolated local Kestrel host. [Protocol tests](../grpc/test/GrpcEndpointProtocolTest.cs) cover shorthand, plugin registration and missing extensions. Requests with a gRPC media type verify scope dispatch, not complete gRPC calls.

Debug references local Core output; the project file defines the minimum Release Core package dependency. Supply the corresponding Core build containing the network value APIs before publishing Web. Local Release validation uses an isolated package feed and does not imply publication. See [Plugins.Web](../../Zongsoft.Plugins.Web/README.md) for plugin host integration. Web.Grpc validates regional services, reflection and streaming in its own HTTP/2 tests. Diagnostics clients, Nginx and actual HTTP/3 operation have separate integration validation steps.

See Microsoft's [Kestrel endpoint documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints) for configuration loading and [ASP.NET Core routing documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing) for the route policy extension points.


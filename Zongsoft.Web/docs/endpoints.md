# Web endpoints and request scopes

[English](endpoints.md) | [简体中文](endpoints.zh-Hans.md)

`Zongsoft.Web.Hosting` provides startup endpoint snapshots, Kestrel configuration projection and request-scope constraints. Hosts must explicitly connect these facilities after configuration loading. Referencing the assembly or adding a binding processing instruction does not install the pipeline. Plugin host integration and gRPC service registration belong to Plugins.Web and Web.Grpc.

## Host integration

Place this fragment after an existing ASP.NET Core WebApplicationBuilder has loaded its final configuration. Kestrel must have valid configured endpoints.

```csharp
using Zongsoft.Web.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

var endpoints = builder.Services.AddWebEndpoints(
	builder.Configuration.GetSection("Kestrel"));

builder.WebHost.ConfigureKestrel(endpoints.Configure);
builder.Services.Configure<SocketTransportOptions>(endpoints.Configure);

var app = builder.Build();
app.UseWebEndpoints();
app.UseRouting();
// Add authentication, authorization, static files and existing routes afterwards.
```

`AddWebEndpoints` returns the registered `WebEndpointCollection` for final Kestrel configuration. Install its projection after other Kestrel callbacks and before original endpoints load. Do not load the original URLs with paths first or add unregistered listeners through Listen, urls or environment variables. On configuration changes, the host uses `HasChanged(kestrelSection)` to report that a restart is required, without reconfiguring listeners or routes.

Install `UseWebEndpoints` once, before routing, forwarded headers and ordinary HTTP middleware that may return early. It does not replace authentication or authorization. Scope constraints govern listener and route ownership; arbitrary custom middleware remains the application's responsibility. Ordinary requests cannot reach static files or ordinary routes through a gRPC-only scope.

## Snapshot and projection

Each configuration item supplies a complete `Url` and `Protocols`, for example:

```ini
Kestrel:Endpoints:metrics:Url=http://127.0.0.1:4317/opentelemetry
Kestrel:Endpoints:metrics:Protocols=grpc+http2
Kestrel:Endpoints:orders:Url=http://127.0.0.1:4317/orders
Kestrel:Endpoints:orders:Protocols=grpc+http2
```

These logical scopes share one physical HTTP/2 listener. Original configuration retains complete URLs; projected URLs contain only the scheme, host and port. Certificates, SNI and connection defaults are preserved. Items sharing a socket must have identical connection settings. Overlapping listeners, duplicate same-kind scopes, same-kind paths differing only in case, and mixed HTTP versions on a cleartext socket fail. Native SNI Protocols cannot expand the declared versions.

`WebEndpoint` exposes the configuration name, `NetworkAddress`, a read-only `NetworkProtocol` collection and aggregate HTTP carrier versions in `HttpProtocols`. `GetProtocols(kind)` returns the versions declared for a request kind, defaulting to `http`. The collection sorts by descending path length, then ordinal name. Missing declarations do not inherit implicit server capabilities; an explicitly configured `EndpointDefaults:Protocols` can supply the endpoint's default declaration.

The Web library supplies ordinary HTTP rules. Register upper-level protocols through `AddWebEndpointProtocol` before creating the snapshot. Valid syntax does not imply an installed runtime policy. Missing protocols, duplicate names or independent kinds, and registration after snapshot creation fail without ordinary HTTP fallback. Registering the same instance again before snapshot creation is harmless. The optional second argument of `WebEndpointCollection` also accepts policy instances directly. The registration collection is copied; policy instances must remain immutable or thread-safe.

Encoding protocols may share ordinary HTTP. For example, `services.AddWebEndpointProtocol(new WebEndpointProtocol("protobuf"));` declares `protobuf → protobuf+http`; `protobuf,protobuf+http2` normalizes to `protobuf+http2`. This declaration does not install a serializer. Runtime listening requires a specific HTTP version: `http` or the expanded `protobuf+http` alone is insufficient. Direct TCP/UDP combinations are not carried by this Kestrel adapter.

Native gRPC policy belongs to the separate `Zongsoft.Web.Grpc` package. Explicit hosts call its `services.AddGrpcWebEndpoints()` before `AddWebEndpoints`. The plugin's `GrpcInitializer.Registration` registers this policy automatically; complete snapshot integration with the plugin host belongs to the subsequent Plugins.Web work. This policy expands bare `grpc` to `grpc+http2`, accepts HTTP/2 and HTTP/3 carriers, and rejects unsuitable carriers. The Web library contains no gRPC name checks.

An HTTPS `grpc+http3` declaration can produce an HTTP/3 projection. Snapshot creation does not probe the backend or QUIC support. Actual startup remains subject to Kestrel, TLS and platform requirements. HTTP/3 runtime validation is separate from TCP socket validation.

Hosts may be IP addresses, wildcards or localhost; other DNS names are not listener addresses. * explicitly means IPv4. Managed IPv6 TCP sockets set DualMode=false before Bind, while unmanaged sockets retain the original factory. This callback does not configure QUIC UDP sockets. Kestrel expands localhost to IPv4/IPv6 loopback.

## Paths and request kinds

Declared base paths use ASCII letters, digits and -._~ in segments. Empty interior segments, standalone dot segments, escapes, templates and wildcards fail. Case and trailing slashes remain significant. `/metrics` matches itself and children; `/metrics/` does not match `/metrics`.

The pipeline reads actual Connection.LocalIpAddress/LocalPort, connection protocol and IHttpRequestFeature.RawTarget. Host and forwarded headers do not select listeners. Raw paths exclude queries but preserve percent escapes and dot segments. Absolute-form targets retain their original path; OPTIONS * bypasses path mapping. Invalid targets return 400.

Content-Type determines the request kind before longest-prefix selection. Native gRPC includes application/grpc and application/grpc+format, case-insensitively and with media-type parameters. grpc-web is excluded. HTTP/1 gRPC requests return 400; non-POST requests return 405, without ordinary HTTP fallback. An ordinary request without a scope returns 404. A gRPC request without a scope receives an empty trailers-only response with HTTP 200 and grpc-status=12.

After selection, an unsupported version, unknown method or routing failure does not select a shorter scope. If ASP.NET's parsed path enters a different scope or leaves the selected scope, the pipeline returns 404 before ordinary handlers instead of dispatching again. Business parameters may still be decoded within the same scope. This pipeline does not rewrite the body, query, headers, cancellation or original request path.

## Route constraints

`WebEndpointFeature` retains the selected endpoint, raw path and `Kind`. `WebEndpointMetadata` associates an ASP.NET route endpoint with the exact base path and kind:

```csharp
routes.MapGroup("/opentelemetry")
	.WithMetadata(new WebEndpointMetadata("/opentelemetry", "grpc"));
```

The gRPC extension must register services and a regional fallback within that group. Unannotated routes participate only in ordinary HTTP. Annotated routes must match the selected scope exactly. Fixed gRPC method routes additionally require the exact raw full path, including case and encoding. A shorter scope's fallback cannot handle unknown methods in a child scope. The Web library adds no gRPC package. Handler mounting, regional reflection and regional fallback remain part of the subsequent Web.Grpc service-mounting work.

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

These facilities consume Kestrel runtime configuration. They do not interpret binding processing instructions, generate external entrances or prepend paths to controllers. They project declarations with paths into physical listeners and enforce logical scopes at request time. Ordinary HTTP can likewise use separate `/api` and `/admin` listener scopes. Deployment tools consume binding exposure declarations separately to generate proxy mappings.

## Validation and delivery

[Snapshot tests](../test/Hosting/WebEndpointCollectionTest.cs) cover generic protocols, shared listeners, conflicts, independent snapshots and IPv6 TCP binding. [Ordinary pipeline tests](../test/Hosting/WebEndpointPipelineTest.cs) verify independent operation, kinds and registration without referencing the gRPC extension. [gRPC pipeline tests](../grpc/test/GrpcEndpointPipelineTest.cs) cover media types, raw paths, isolation and HTTP/1 plus HTTP/2 requests against an isolated local Kestrel host. [Protocol tests](../grpc/test/GrpcEndpointProtocolTest.cs) cover shorthand, plugin registration and missing extensions. Requests with a gRPC media type verify scope dispatch, not complete gRPC calls.

Debug references local Core output; the project file defines the minimum Release Core package dependency. Supply the corresponding Core build containing the network value APIs before publishing Web. Local Release validation uses an isolated package feed and does not imply publication. Plugin loading, complete gRPC services/reflection/streaming, Nginx and actual HTTP/3 operation require subsequent integration validation.

See Microsoft's [Kestrel endpoint documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints) for configuration loading and [ASP.NET Core routing documentation](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing) for the route policy extension points.


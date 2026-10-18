# Zongsoft.Diagnostics.Protocols.Client Client Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Client)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Client)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**C**lient](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/client) is the [_**g**RPC_](https://grpc.io) client plugin library for diagnostics and telemetry in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides [_**g**RPC_](https://grpc.io) client features for diagnostics and telemetry based on [_**O**pen**T**elemetry_](https://opentelemetry.io).

The package contains OTLP C# message types and generated gRPC clients for the logs, metrics, and traces collector services. It is a protocol assembly: it does not collect telemetry, batch it, retry exports, or configure an OpenTelemetry SDK pipeline.

## Installation and Scope

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Client
```

Use it for direct calls to OTLP collector services or when another component needs the generated protocol types. For normal instrumentation, prefer the official OpenTelemetry .NET SDK described in the [parent guide](../../README.md).

The build generates messages for the imported schema and client stubs for collector `*_service.proto` contracts. Principal namespaces are `OpenTelemetry.Proto.Collector.Logs.V1`, `.Metrics.V1`, `.Trace.V1`, and their data-message namespaces.

## Direct Client Pattern

```csharp
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using Zongsoft.Diagnostics.Protocols.Client;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var address = new Uri("https://collector.example.invalid/opentelemetry");
using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
	HttpHandler = new GrpcClientHandler(address.AbsolutePath, new SocketsHttpHandler()),
	DisposeHttpClient = true,
});
var client = new MetricsService.MetricsServiceClient(channel);
var response = await client.ExportAsync(
	new ExportMetricsServiceRequest(),
	cancellationToken: cancellation.Token);
```

`GrpcClientHandler` adds the declared base path to the generated RPC path. The path must match the receiver; the bundled server uses `/opentelemetry`. A plain `GrpcChannel` does not apply the path in its address; this follows the [official subdirectory guidance](https://learn.microsoft.com/aspnet/core/grpc/troubleshoot#calling-grpc-services-hosted-in-a-sub-directory).

The handler accepts `/` or an absolute literal path containing ASCII letters, digits, `-._~` and separators. One trailing slash is allowed; empty interior segments, `.` or `..` segments, encoded characters, queries and fragments are rejected. `Path` retains the declared spelling; joining with the native RPC path uses one slash. Each handler chain may contain only one instance, and a request cannot have the prefix applied twice. The handler preserves the authority, escaped request path and query, headers, content, HTTP version, cancellation and response; it does not buffer messages or choose a transport. It supports unary and streaming calls.

The caller supplies the transport handler, including any proxy, TLS or HTTP/2 settings. Disposing `GrpcClientHandler` disposes its inner handler. In the example, `DisposeHttpClient = true` transfers disposal to the channel; when sharing a handler or HTTP client, retain ownership and dispose it after all channels stop. See [client options](https://learn.microsoft.com/aspnet/core/grpc/configuration#configure-client-options).

Official OpenTelemetry exporters already append their RPC path to `Endpoint`; configure the complete endpoint, such as `http://localhost:4317/opentelemetry`, without installing `GrpcClientHandler` in that pipeline. This handler is for direct generated-client calls.

Populate messages according to the [OTLP specification](https://opentelemetry.io/docs/specs/otlp/). The empty request demonstrates call shape only; a useful exporter must attribute telemetry correctly and handle partial success.

🚨 Do not disable TLS validation or embed credentials. Configure deadlines, message limits, proxy behavior, retry/batching and backpressure, and always propagate cancellation.

Generated types reflect the protocol revision pinned by this repository. Protobuf can remain wire-compatible while generated source APIs change; do not use these messages as an application-owned long-term persistence format.

## Local Verification

Run from the repository root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Diagnostics/protocols/client/test/unit/Zongsoft.Diagnostics.Protocols.Client.Tests.csproj -p:GeneratePackageOnBuild=false
```

Unit tests exercise path validation, duplicate installation, query and request preservation through public send APIs and in-memory handlers. The unit test project has no ASP.NET Core dependency and does not start a server.

The separate integration test project uses a local HTTP/2 server to verify unary and all three streaming call types, large messages, metadata, status, trailers, cancellation, deadlines and channel ownership of HttpHandler/HttpClient. Run it explicitly:

```shell
dotnet test Zongsoft.Diagnostics/protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx -p:GeneratePackageOnBuild=false
```

ASP.NET Core and the test-only gRPC service belong to the integration project. The client library does not reference Web hosting projects; the normal client solution and Cake `test` task exclude integration tests.

## Related Resources

- [Diagnostics overview](../../README.md)
- [Server package](../server/README.md)
- [OTLP specification](https://opentelemetry.io/docs/specs/otlp/)
- [gRPC .NET client guidance](https://learn.microsoft.com/aspnet/core/grpc/client)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

This manifest makes the generated protocol assembly available; it does not create an exporter or send telemetry automatically. Construct/configure the client as described above.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Diagnostics.Protocols.Client` | [Zongsoft.Diagnostics.Protocols.Client.plugin](src/Zongsoft.Diagnostics.Protocols.Client.plugin) |
| File copying and dependencies | [Zongsoft.Diagnostics.Protocols.Client.deploy](src/Zongsoft.Diagnostics.Protocols.Client.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft diagnostics protocols client]
nuget:Zongsoft.Diagnostics.Protocols.Client
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Diagnostics.Protocols.Client.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

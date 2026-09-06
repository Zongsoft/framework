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

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
using var channel = GrpcChannel.ForAddress("https://collector.example.invalid");
var client = new MetricsService.MetricsServiceClient(channel);
var response = await client.ExportAsync(
	new ExportMetricsServiceRequest(),
	cancellationToken: cancellation.Token);
```

Populate messages according to the [OTLP specification](https://opentelemetry.io/docs/specs/otlp/). The empty request demonstrates call shape only; a useful exporter must attribute telemetry correctly and handle partial success.

🚨 Do not disable TLS validation or embed credentials. Configure deadlines, message limits, proxy behavior, retry/batching and backpressure, and always propagate cancellation.

Generated types reflect the protocol revision pinned by this repository. Protobuf can remain wire-compatible while generated source APIs change; do not use these messages as an application-owned long-term persistence format.

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

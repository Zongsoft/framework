# Zongsoft.Diagnostics.Protocols.Server Server Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Server)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Server)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**S**erver](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/server) is the [_**g**RPC_](https://grpc.io) server plugin library for diagnostics and telemetry in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides [_**g**RPC_](https://grpc.io) server features based on [_**O**pen**T**elemetry_](https://opentelemetry.io), allowing telemetry data to be listened for and processed through the [_**g**RPC_](https://grpc.io) protocol. See the [samples](./samples/) for examples.

It implements the OTLP logs, metrics, and traces collector services, converts generated protobuf messages into Zongsoft telemetry models, and fans each accepted batch out to registered `IHandler` implementations.

## Installation and Endpoint

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Server
```

Deploy the plugin and option artifacts. The supplied option defines an HTTP/2 Kestrel endpoint named `telemetry` at `http://*:4317`, the conventional OTLP/gRPC port. Override that endpoint in deployment configuration when binding, TLS, or network policy differs.

🚨 The default wildcard plaintext listener is suitable only for a trusted development network. Production deployments should bind deliberately, enable TLS or terminate it at a trusted proxy, authenticate senders, and firewall the endpoint.

## Processing Model

| OTLP signal | Service singleton | Handler argument |
| --- | --- | --- |
| logs | `Listener.Logs` | converted Zongsoft log collections |
| metrics | `Listener.Metrics` | `IEnumerable<Meter>` |
| traces | `Listener.Traces` | converted trace collections |

Register handlers on the relevant processor's `Handlers` collection, normally through the plugin tree. Each batch is dispatched to handlers concurrently with `Parallel.ForEachAsync`. A handler exception is logged and isolated so another handler can still run; request cancellation is propagated.

The following comes from the [actual sample manifest](samples/Zongsoft.Diagnostics.Protocols.Server.Samples.plugin). Its [MetricHandler](samples/MetricHandler.cs) only prints metrics to the terminal; deploy the sample assembly, and do not treat it as a persistence handler.

```xml
<extension path="/Workbench/Diagnostics/Telemetry/Listener/Metrics">
	<object name="MetricHandler" type="Zongsoft.Diagnostics.Protocols.Server.Samples.MetricHandler, Zongsoft.Diagnostics.Protocols.Server.Samples" />
</extension>
```

See the [sample plugin](samples/README.md) for a concrete `HandlerBase<IEnumerable<Meter>>` implementation and deployment steps.

## Operational Limits

The processors currently return a successful empty OTLP response after dispatch and log handler failures. If durable ingestion is required, handlers must explicitly enqueue or persist before returning and define their own overload strategy. Bound queue sizes and execution time; slow handlers extend the export call and uncontrolled parallel work can exhaust resources.

Timestamp conversion uses OTLP Unix nanoseconds with millisecond precision in the framework model. Validate attribute types, histogram/summary semantics, and data loss expectations against the pinned [OTLP specification](https://opentelemetry.io/docs/specs/otlp/).

## Related Resources

- [Diagnostics overview](../../README.md)
- [Client protocol package](../client/README.md)
- [Server sample](samples/README.md)
- [OTLP exporter configuration](https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

Use a Web host and deploy `Zongsoft.Web.Grpc` explicitly. Register handlers under `/Workbench/Diagnostics/Telemetry/Listener` and configure the HTTP/2 endpoint; the protocol DLL alone does not start a receiver.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Diagnostics.Protocols.Server` | [Zongsoft.Diagnostics.Protocols.Server.plugin](src/Zongsoft.Diagnostics.Protocols.Server.plugin) |
| File copying and dependencies | [Zongsoft.Diagnostics.Protocols.Server.deploy](src/Zongsoft.Diagnostics.Protocols.Server.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft web grpc]
nuget:Zongsoft.Web.Grpc

[plugins zongsoft diagnostics protocols server]
nuget:Zongsoft.Diagnostics.Protocols.Server
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Diagnostics.Protocols.Server.option`, `Zongsoft.Diagnostics.Protocols.Server.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

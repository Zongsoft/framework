# Zongsoft.Diagnostics.Protocols.Server Server Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Server)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Server)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**S**erver](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/server) is the [_**g**RPC_](https://grpc.io) server plugin library for diagnostics and telemetry in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides [_**g**RPC_](https://grpc.io) server features based on [_**O**pen**T**elemetry_](https://opentelemetry.io), allowing telemetry data to be listened for and processed through the [_**g**RPC_](https://grpc.io) protocol. See the [samples](./samples/) for examples.

It implements the OTLP logs, metrics, and traces collector services and dispatches accepted requests to registered `IHandler` implementations. Metrics are converted to framework meter collections; logs and traces currently expose the original request through `Parameters` and pass a null handler argument.

## Installation and Endpoint

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Server
```

Deploy the plugin and option artifacts. The supplied option declares `telemetry` at `http://*:4317/opentelemetry` with `protocols="grpc+http2"`. Each concrete processor carries `[Handler("/opentelemetry")]`. Plugins.Web projects the complete URL into the physical Kestrel listener, and Web.Grpc mounts all three services under that path. Keep the handler path and declared endpoint path identical; changing the deployment address does not change the handler attribute.

🚨 The default wildcard plaintext listener is suitable only for a trusted development network. Production deployments should bind deliberately, enable TLS or terminate it at a trusted proxy, authenticate senders, and firewall the endpoint.

The source configuration is [Zongsoft.Diagnostics.Protocols.Server.option](src/Zongsoft.Diagnostics.Protocols.Server.option):

```xml
<?zongsoft.tools.binding name="telemetry"
	path="Kestrel/Endpoints/telemetry"
	template="${this.Url}"
	protocols="${this.Protocols}"
	required="true"?>
<options>
	<option path="kestrel">
		<endpoints>
			<telemetry url="http://*:4317/opentelemetry" protocols="grpc+http2" />
		</endpoints>
	</option>
</options>
```

Set the official OpenTelemetry exporter's Endpoint to the complete address. Direct generated clients use [GrpcClientHandler](../client/README.md#direct-client-pattern). Both send `/opentelemetry/<native-service-name>/<method-name>`; a proxy must preserve that complete path. Reflection is mounted in the same region. Calls without the prefix or with an incorrect prefix do not reach business handlers; unknown RPCs within the region return UNIMPLEMENTED.

## Processing Model

| OTLP signal | Service singleton | Handler argument |
| --- | --- | --- |
| logs | `Listener.Logs` | null; original `ExportLogsServiceRequest` in `Parameters` |
| metrics | `Listener.Metrics` | `IEnumerable<Meter>` |
| traces | `Listener.Traces` | null; original `ExportTraceServiceRequest` in `Parameters` |

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

## Local Verification

Unit tests check the endpoint and binding declarations without starting a server or referencing ASP.NET Core. Build Core first, then run from the repository root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Diagnostics/protocols/server/test/unit/Zongsoft.Diagnostics.Protocols.Server.Tests.csproj -p:GeneratePackageOnBuild=false
```

The separate integration test project references only the server project through `ProjectReference`. Other Zongsoft dependencies, including the client and sample, use local assemblies through `HintPath`. Build them first in the same configuration and target frameworks. Run these Debug commands from the repository root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Web/grpc/src/Zongsoft.Web.Grpc.csproj -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Diagnostics/protocols/client/src/Zongsoft.Diagnostics.Protocols.Client.csproj -p:GeneratePackageOnBuild=false
dotnet build Zongsoft.Diagnostics/protocols/server/samples/Zongsoft.Diagnostics.Protocols.Server.Samples.csproj -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Diagnostics/protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx -p:GeneratePackageOnBuild=false
```

Integration tests use isolated child processes with a plain `WebApplication`, explicit Kestrel endpoints, registered gRPC services and the existing sample handler. They do not load a plugin tree; plugin-host composition is covered in Plugins.Web. They check payloads for all three signals from the official SDK and generated clients, sample metric output, regional reflection, incorrect paths, cancellation propagation and port release after shutdown. All network traffic stays on loopback; this does not verify a proxy or production-host deployment.

The normal server solution and Cake `test` task run only unit tests. The [server integration solution](test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx) and [client integration solution](../client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx) each live beside their integration project. After preparing the dependencies above, run `dotnet cake build.cake --target=integration-test` from the `Zongsoft.Diagnostics` directory to build and run both.

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

# Zongsoft.Diagnostics Diagnostics Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**iagnostics](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics) is the diagnostics plugin library for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides diagnostics and telemetry features based on [_**O**pen**T**elemetry_](https://opentelemetry.io).

The [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) configuration file defines the default _**O**pen**T**elemetry_ metric and trace exporters.

Use `driver="opentelemetry"` for OTLP export. Set `server` to the receiver endpoint and `protocol` to `grpc` or `HttpProtobuf`; the endpoint must accept the selected OTLP transport. The bundled Diagnostics receiver uses `/opentelemetry`, so its paired exporter configuration uses `server=http://localhost:4317/opentelemetry;protocol=grpc`. Official SDK exporters use this full endpoint directly; direct generated clients use the [client base-path handler](protocols/client/README.md#direct-client-pattern).

## Enable or disable exporters

The code entry points are `Zongsoft.Diagnostics.Telemetry.ExporterLauncher.Default` (OTLP) and `ExporterLauncher.Console`. Both expose `Meters`, `Tracers`, and `Logs` launcher instances. The host discovers them through assembly scanning; configuration uses `driver="opentelemetry"` or `driver="opentelemetry.console"`. The private registration class in [ExporterLauncher.cs](src/Telemetry/ExporterLauncher.cs) connects logging automatically.

Each metric or trace exporter accepts `enabled="true|false"`. Omission defaults to `true`, preserving existing configurations. `enabled="false"` excludes that exporter before driver resolution and provider construction, so disabled exporters do not require a registered driver or parse their connection settings. If every exporter for a signal is disabled, its provider is not created. The XML and its boolean values must still be valid configuration.

For example, retain a shared OTLP declaration in Web configuration while disabling its metric export:

```xml
<exporter exporter.name="telemetry" driver="opentelemetry" enabled="false"
	settings="server=http://127.0.0.1:4317;protocol=grpc;" />
```

This switch is independent for metrics and traces. The Web plugin can still contribute an enabled Prometheus exporter to the same metrics configuration.

## Strongly typed exporter settings

`settings` remains semicolon-delimited text. The string contracts of `FilteringExporter.Settings` and `IExporterLauncher.Launch` are unchanged. Internally, launchers use [ExporterLauncherSettingsDriver](src/Configuration/ExporterLauncherSettingsDriver.Default.cs) to create typed settings, with SDK option projection kept in `Telemetry/ExporterLauncher.cs`.

| Driver name | Settings type | Driver entry |
| --- | --- | --- |
| `opentelemetry` | `ExporterLauncherSettings` | `ExporterLauncherSettingsDriver.Default` |
| `opentelemetry.console` | `ExporterLauncherSettings.Console` | `ExporterLauncherSettingsDriver.Console` |

Both settings types live in `Zongsoft.Diagnostics.Configuration` and independently inherit the connection-settings base; Console does not inherit OTLP properties. Private drivers expose instances through typed public interfaces. The `.plugin` explicitly registers both under `/Workbench/Configuration/ConnectionSettings/Drivers`, without relying on discovery of an `Instance` member. Direct API calls need no plugin host:

```csharp
var settings = ExporterLauncherSettingsDriver.Default.GetSettings(
	"server=http://localhost:4317;protocol=grpc;timeout=30s;interval=5s;");
var consoleSettings = ExporterLauncherSettingsDriver.Console.GetSettings(
	"target=Console,Debug;interval=10s;");
```

| Parameter | Type | Default and purpose |
| --- | --- | --- |
| `server` | `string` | OTLP endpoint; omission selects `http://localhost:4317` or `http://localhost:4318` according to protocol |
| `protocol` | `OtlpExportProtocol` | OTLP: `Grpc`; also supports `HttpProtobuf` |
| `processorType` | `ExportProcessorType` | OTLP: `Batch`; also supports `Simple`, selecting the log/trace processor |
| `timeout` | `TimeSpan` | `30s`; OTLP export/batch timeout and Console metric-reader timeout |
| `interval` | `TimeSpan` | `10s`; metric interval and OTLP batch delay |
| `maxBatchSize` | `int` | OTLP batch: `512` |
| `maxQueueSize` | `int` | OTLP batch: `2048` |
| `target` | `ConsoleExporterOutputTargets` | Console: `Console`; also supports `Debug` or `Console,Debug` |

Keys are case-insensitive. Durations support connection-settings formats such as `1500ms` and `5s`; typed defaults are not inserted into the original `Value`. Unknown keys remain in the dictionary, following the base settings contract; they do not add exporter behavior.

**An absent or entirely empty `settings` leaves SDK defaults and environment configuration untouched.** The defaults above apply to omitted parameters in nonempty settings text. Invalid typed values read by an enabled exporter now fail rather than silently using defaults. Projection also validates enums, absolute HTTP/HTTPS endpoints, durations between 1 and 2147483647 milliseconds, positive batch/queue sizes, and `maxBatchSize <= maxQueueSize`. An explicitly empty `server` is invalid. The existing OTLP projection reads processor and batch settings for all three signals; they only control log/trace processing. Console logs and traces read only `target`, ignoring metric-only timing parameters. Disabled exporters do not parse settings.

New configuration uses `opentelemetry` and `opentelemetry.console`. Launchers continue accepting the case-insensitive aliases `telemetry`, `OpenTelemetry`, and `Console`; OTLP also retains empty-driver matching. `exporter.name="telemetry"` is the configuration entry name, distinct from its driver. Existing host variables and switches under `diagnostics.telemetry` and `diagnostics.logging` retain their names.

## Microsoft ILogger logs

During service registration, the plugin reads `/Diagnostics/Diagnostor/Logs` and connects `Microsoft.Extensions.Logging.ILogger` to OpenTelemetry. Hosts need no `Program.cs` changes or ASP.NET Core dependency. Zongsoft's own `Logging`/`ILogger` and `Console.WriteLine` are outside this pipeline.

Logs are disabled by default. To enable them, place this node inside `<diagnostor>`:

```xml
<logs includeScopes="true" includeFormattedMessage="true">
	<exporters>
		<exporter exporter.name="telemetry" driver="opentelemetry" enabled="true"
			settings="server=http://127.0.0.1:4317;protocol=grpc;processorType=batch;timeout=30s;interval=5s;" />
	</exporters>
</logs>
```

For local debugging, add a Console log exporter inside `<logs><exporters>`. It can coexist with an OTLP exporter:

```xml
<exporter exporter.name="console" driver="opentelemetry.console" enabled="true" settings="target=Console;" />
```

The `target` setting follows the metric and trace Console exporters: it defaults to `Console` and also supports `Debug` or `Console,Debug`. Logs are exported individually; `interval`, `timeout`, and batch settings do not apply. This is OpenTelemetry's detailed log output. If the host also enables the standard Console logging provider, the same log may appear twice.

Logging is independent of metrics and tracing. If `<logs>` is absent, has no exporters, or all exporters have `enabled="false"`, no logging provider is registered. Disabled entries do not resolve drivers or parse settings. A missing enabled driver fails startup with `Signal=logs` and `ExporterDriver` in exception Data.

Categories and levels follow standard host `Logging:LogLevel` settings, including provider-specific `Logging:OpenTelemetry:LogLevel` rules. There is no additional `<filter>`. `includeScopes` and `includeFormattedMessage` default to true. The SDK exports structured attributes, exceptions and the current Activity's TraceId/SpanId. Startup logs without an Activity normally have no trace identifiers.

The log resource uses `IHostEnvironment.ApplicationName` (or the current application domain name without a host environment), with `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` overrides. Metrics, traces and logs in the same process share the default `service.instance.id`. Host DI owns the provider and flushes pending batches on normal disposal; the metric/trace worker does not start or stop logging. Configuration changes require a host restart, and logs written before registration are not replayed. Existing OpenTelemetry dependencies include logging; plugin manifests and deployment dependencies need no ASP.NET Core additions.

The receiver must support OTLP logs. Aspire Dashboard accepts them directly. The existing metrics-only Collector example needs a separate logs pipeline and log backend; Prometheus does not store logs.

## General and Web hosts

The common library has no ASP.NET Core dependency. Daemon and terminal deploy only this package, sending OTLP metrics to a Collector that Prometheus scrapes. Direct Web scraping moved to [Zongsoft.Diagnostics.Web](../Zongsoft.Diagnostics.Web/README.md), deployed only to Web hosts.

A Collector normally accepts `server=http://127.0.0.1:4317;protocol=grpc`, without the Zongsoft receiver's `/opentelemetry` prefix. Collector and Prometheus YAML configuration files are read by their respective services. Containers require service names and reachable listening addresses.

The worker sets `service.name` from the application name; the SDK generates a process `service.instance.id` shared by metrics and traces. `OTEL_SERVICE_NAME` and `OTEL_RESOURCE_ATTRIBUTES` override defaults. A missing enabled exporter fails startup with InvalidOperationException and ExporterDriver/Signal data. Partial startup releases already-created providers.

Build Core first, then run `dotnet test test/Zongsoft.Diagnostics.Tests.csproj -p:GeneratePackageOnBuild=false` to check the ordinary .NET runtime, exporter registration, resource identity, exported metrics/traces and lifecycle.

`Zongsoft.Diagnostics.slnx` and the default Cake `test` task include only unit tests. Protocol integration tests live in separate `protocols/client/test/integration` and `protocols/server/test/integration` projects. Each integration project has its own solution in the same directory: [client](protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx) and [server](protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx). Prepare the dependencies in [Local Verification](protocols/server/README.md#local-verification), then run `dotnet cake build.cake --target=integration-test` from this directory to exercise the local HTTP/2 and OTLP services explicitly.

## Bundled OpenTelemetry Protocol

The [`proto`](proto) directory is a Git submodule pinned to the upstream [OpenTelemetry Protocol repository](https://github.com/open-telemetry/opentelemetry-proto). It contains the [OTLP protocol specification](proto/docs/specification.md) and the corresponding language-independent interface types ([`.proto` files](proto/opentelemetry/proto)). Keep this submodule clean: protocol documentation and project-specific guidance belong in this README rather than in the upstream working tree.

### Language-Independent Interface Types

The Proto files can be consumed as a Git submodule or copied and built directly in a consumer project. OpenTelemetry client libraries publish compiled artifacts to central repositories such as Maven. Changes to the upstream definitions must follow the [OpenTelemetry Proto contribution guidelines](proto/CONTRIBUTING.md).

### OTLP/JSON

Additional requirements for the OTLP/JSON wire representation are defined by the [JSON Protobuf encoding specification](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md#json-protobuf-encoding).

### Generating gRPC Client Libraries

The upstream repository generates raw gRPC client libraries with `make gen-${LANGUAGE}`. Supported targets currently include:

- cpp
- csharp
- go
- java
- objc
- openapi (Swagger)
- php
- python
- ruby

### Maturity

Releases numbered 1.0.0 and later may still contain unstable alpha or beta components, as indicated below.

| Component | Binary Protobuf maturity | JSON maturity |
| --- | --- | --- |
| common/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| resource/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| metrics/\*<br>collector/metrics/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| trace/\*<br>collector/trace/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| logs/\*<br>collector/logs/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| profiles/\*<br>collector/profiles/* | Development | [Development](proto/docs/specification.md#json-protobuf-encoding) |

See [Versioning and Stability](https://github.com/open-telemetry/opentelemetry-specification/blob/a08d1f92f62acd4aafe4dfaa04ae7bf28600d49e/specification/versioning-and-stability.md) for the definition of maturity levels.

### Stability Guarantees

Components marked `Stable` guarantee that existing field types, numbers, and names; service and package names; method names, parameters, return types, and invocation kinds; message and enum symbols; package names and directory structure; and existing `optional` or `repeated` declarations will not change incompatibly. Existing symbols will not be deleted.

Compatible additive changes may introduce:

- New fields in existing messages.
- New messages or enums.
- New enum choices.
- New choices in existing `oneof` fields.
- New services.
- New methods in existing services.

Every additive change must explain how senders and receivers implemented against protocol versions from before and after the change interoperate.

### Experiments

New experimental components should be isolated in a `development` subdirectory and use `Development`, `Alpha`, `Beta`, or `Release Candidate` maturity labels. Experimental components that are not referenced by a stable component may be removed when an experiment ends. Successful experiments require review before they become `Stable` and inherit the full stability guarantees.

Experimental fields or messages added to stable components must remain compatible and be clearly marked as non-stable. If an experiment is abandoned, those fields or messages must remain in place and may only be deprecated; senders should normally leave them empty, and receivers must continue to tolerate empty values.

### Generated Code

The upstream project does not guarantee the stability of code produced by any particular Proto code generator.

### Upstream Maintainers and Approvers

- Maintainers: [OpenTelemetry Technical Committee](https://github.com/open-telemetry/community/blob/main/community-members.md#technical-committee)
- Approvers: [OpenTelemetry Specification Sponsors](https://github.com/open-telemetry/community/blob/main/community-members.md#specifications-and-proto)

See the OpenTelemetry community [membership guide](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#maintainer) for role details.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The plugin contributes diagnostics configuration and a startup component. Select filters/exporters in its same-stem `.option`; loading it can activate telemetry exporters, so use controlled destinations.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Diagnostics` | [Zongsoft.Diagnostics.plugin](src/Zongsoft.Diagnostics.plugin) |
| File copying and dependencies | [Zongsoft.Diagnostics.deploy](src/Zongsoft.Diagnostics.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft diagnostics]
nuget:Zongsoft.Diagnostics
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Diagnostics.option`, `Zongsoft.Diagnostics.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

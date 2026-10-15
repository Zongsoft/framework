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

The [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) configuration file defines the default _**O**pen**T**elemetry_ metric and trace exporters, together with the [_**P**rometheus_](https://prometheus.io) metric exporter.

Use `driver="telemetry"` for OTLP export. Set `server` to the receiver endpoint and `protocol` to `grpc` or `HttpProtobuf`; the endpoint must accept the selected OTLP transport. The bundled Diagnostics receiver uses `/opentelemetry`, so its paired exporter configuration uses `server=http://localhost:4317/opentelemetry;protocol=grpc`. Official SDK exporters use this full endpoint directly; direct generated clients use the [client base-path handler](protocols/client/README.md#direct-client-pattern).

## Prometheus listener

The `prometheus` exporter accepts HTTP scrape requests. Its `urls` setting contains the complete listening prefixes, including the scrape path, with an optional trailing slash. The binding declaration reads those same addresses and declares this launcher's fixed HTTP/1 capability with a constant template:

```xml
<!-- Zongsoft.Diagnostics/src/Zongsoft.Diagnostics.option; listener-related fragment -->
<?binding name="prometheus"
	path="Diagnostics/Diagnostor/Meters/Exporters/*"
	filter="Driver:prometheus"
	template="${option.Settings['urls']}"
	protocols="http1"?>
<options>
	<option path="/Diagnostics">
		<diagnostor>
			<meters>
				<filter>*</filter>
				<exporters>
					<exporter exporter.name="prometheus" driver="prometheus"
						settings="urls=http://127.0.0.1:9464/metrics,http://localhost:9464/metrics;totalSuffix=true;cacheDuration=300ms;" />
				</exporters>
			</meters>
		</diagnostor>
	</option>
</options>
```

`urls` is required; `protocol` defaults to `http1` when omitted. If specified, it accepts a single case-insensitive `http1` value, for example `urls=http://127.0.0.1:9464/metrics;protocol=http1`. Empty values, protocol lists (including repeated `http1` values), and the `protocols` setting are invalid. The binding uses the constant template `protocols="http1"`, so omitting the setting does not require the packaging tool to infer a default. HTTP/2, HTTP/3 and application protocol combinations are not implemented by this launcher. This is runtime configuration validation, independent of the packaging tool's binding mapping checks.

Each URL uses http or https with an IP address, wildcard, localhost or DNS hostname. All URLs in an exporter must have equivalent base paths, ignoring case and an optional trailing slash: /metrics and /Metrics/ are equivalent; an omitted path means /. Declaration segments allow ASCII letters, digits and -._~; interior empty segments, standalone dot segments, credentials, queries, fragments and encoded paths are rejected. Source configuration stays unchanged; the scrape option removes a non-root trailing slash. An independent Kestrel server matches raw URI paths at segment boundaries, without decoding or collapsing dot segments to select a range. The base and its children can be scraped; /metrics-extra returns 404. No redirects or double-slash workaround are used, and Windows and Linux follow the same rules.

IP declarations retain their interface and address-family scope: `*` and `0.0.0.0` listen on IPv4, `[::]` on IPv6 only, and `localhost` on both available loopback families. Ordinary DNS names are not resolved at startup: they listen on the available IPv4/IPv6 wildcard interfaces and accept requests with the declared HTTP Host name, ignoring case and a final dot. Internationalized names and their ASCII forms are equivalent. For example, `urls=http://metrics.example.com:9464/metrics` accepts that host on port 9464; the caller must resolve it to this server. This is host routing, not authentication.

Multiple declarations can share a physical listener within this exporter. Each request must still match an original declaration's scheme, local port, interface/address family and, for DNS names, Host name. Combining a domain with `127.0.0.1` therefore allows arbitrary Host names on that loopback address only; other local addresses still require the declared domain. Different domains and ports do not grant access to one another.

Separate addresses with `,`, `;` or `|`. Empty entries fail, including trailing separators. Quote the whole `urls` value when using semicolons inside the connection settings, for example `urls='http://127.0.0.1:9464/metrics/;http://localhost:9464/metrics/';`. Remove the separate `path` setting: its presence is an error, even when empty. Missing addresses do not fall back to an implicit listener.

`totalSuffix` defaults to `true`; `cacheDuration` defaults to `300ms`. Both apply to the ASP.NET Core exporter options and the independent listener; a negative cache duration becomes zero and an oversized duration is capped at the supported millisecond range. Registering exporter options does not add an ASP.NET Core route. The diagnostic provider owns the independent listener and releases it on disposal.

The default addresses are loopback addresses. For a reverse proxy in another container, configure a listener reachable from that container; the binding tool does not rewrite loopback addresses. The independent server owns its declared ports and cannot share a socket with another server. HTTPS uses Kestrel:Certificates from IConfiguration in the metrics service container, falling back to ApplicationContext.Current.Configuration. It does not inherit host Endpoints or HTTP.sys certificate bindings. The binding instruction declares exposure intent and does not itself start a listener. Outbound `telemetry` exporters do not participate in this binding.

Focused validation lives in [test](test). Build the solution with `dotnet build Zongsoft.Diagnostics.slnx -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true`, then run `dotnet test test/Zongsoft.Diagnostics.Tests.csproj --no-build`. Tests inspect the public exporter options and exercise temporary HTTP/HTTPS listeners, including domain and IP combinations, IPv4/IPv6 isolation, real metric output, certificate selection, disposal and startup-failure cleanup. Domain requests connect directly to loopback without DNS or hosts-file changes. They do not start a business host or replace reverse-proxy and production HTTPS deployment validation.

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

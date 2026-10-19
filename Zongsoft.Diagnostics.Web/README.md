# Zongsoft.Diagnostics.Web

[English](README.md) | [简体中文](README.zh-Hans.md)

Deploy this plugin only to Web hosts. It requires `Microsoft.AspNetCore.App` and serves metrics from the current process through Kestrel. Daemon and terminal deploy only [Diagnostics](../Zongsoft.Diagnostics/README.md) and send metrics through OTLP → Collector → Prometheus.

The common package owns `/Diagnostics/Diagnostor/Meters/filter` and the single DiagnostorWorker. This plugin adds only the `prometheus` exporter and binding; it does not create another worker or provider. Set `enabled="false"` on the common metric OTLP exporter to avoid duplicate collection. The hosting repositories share one common Diagnostics template: scheme defaults set `diagnostics.telemetry:enabled=true`, and each Web site overrides it to `false` after importing the scheme.

TCP metrics use the same endpoint: explicitly include `Zongsoft.Net` in the common `Meters/filter`. No additional provider or TCP exporter is needed. See the [Net telemetry contract](../Zongsoft.Net/README.md#telemetry-and-logging-policy) for instrument semantics and sampling boundaries; traces and logs use their respective exporters in common Diagnostics.

## Prometheus listener

The `prometheus` exporter accepts HTTP scrape requests. Its `urls` setting contains the complete listening prefixes, including the scrape path, with an optional trailing slash. The binding declaration reads those same addresses and declares this launcher's fixed HTTP/1 capability with a constant template:

```xml
<!-- Zongsoft.Diagnostics.Web/src/Zongsoft.Diagnostics.Web.option; listener-related fragment -->
<?zongsoft.tools.binding name="prometheus"
	path="Diagnostics/Diagnostor/Meters/Exporters/*"
	filter="Driver:prometheus"
	template="${this.Settings['urls']}"
	protocols="http1"?>
<options>
	<option path="/Diagnostics">
		<diagnostor>
			<meters>
				<exporters>
					<exporter exporter.name="prometheus" driver="prometheus"
						settings="urls=http://127.0.0.1:9465/metrics,http://localhost:9465/metrics;totalSuffix=true;cacheDuration=300ms;" />
				</exporters>
			</meters>
		</diagnostor>
	</option>
</options>
```

`urls` is required; `protocol` defaults to `http1` when omitted. If specified, it accepts a single case-insensitive `http1` value, for example `urls=http://127.0.0.1:9465/metrics;protocol=http1`. Empty values, protocol lists (including repeated `http1` values), and the `protocols` setting are invalid. The binding uses the constant template `protocols="http1"`, so omitting the setting does not require the packaging tool to infer a default. HTTP/2, HTTP/3 and application protocol combinations are not implemented by this launcher. This is runtime configuration validation, independent of the packaging tool's binding mapping checks.

Each URL uses http or https with an IP address, wildcard, localhost or DNS hostname. All URLs in an exporter must have equivalent base paths, ignoring case and an optional trailing slash: /metrics and /Metrics/ are equivalent; an omitted path means /. Declaration segments allow ASCII letters, digits and -._~; interior empty segments, standalone dot segments, credentials, queries, fragments and encoded paths are rejected. Source configuration stays unchanged; the scrape option removes a non-root trailing slash. An independent Kestrel server matches raw URI paths at segment boundaries, without decoding or collapsing dot segments to select a range. The base and its children can be scraped; /metrics-extra returns 404. No redirects or double-slash workaround are used, and Windows and Linux follow the same rules.

IP declarations retain their interface and address-family scope: `*` and `0.0.0.0` listen on IPv4, `[::]` on IPv6 only, and `localhost` on both available loopback families. Ordinary DNS names are not resolved at startup: they listen on the available IPv4/IPv6 wildcard interfaces and accept requests with the declared HTTP Host name, ignoring case and a final dot. Internationalized names and their ASCII forms are equivalent. For example, `urls=http://metrics.example.com:9465/metrics` accepts that host on port 9465; the caller must resolve it to this server. This is host routing, not authentication.

Multiple declarations can share a physical listener within this exporter. Each request must still match an original declaration's scheme, local port, interface/address family and, for DNS names, Host name. Combining a domain with `127.0.0.1` therefore allows arbitrary Host names on that loopback address only; other local addresses still require the declared domain. Different domains and ports do not grant access to one another.

Separate addresses with `,`, `;` or `|`. Empty entries fail, including trailing separators. Quote the whole `urls` value when using semicolons inside the connection settings, for example `urls='http://127.0.0.1:9465/metrics/;http://localhost:9465/metrics/';`. Remove the separate `path` setting: its presence is an error, even when empty. Missing addresses do not fall back to an implicit listener.

`totalSuffix` defaults to `true`; `cacheDuration` defaults to `300ms`. Both apply to the ASP.NET Core exporter options and the independent listener; a negative cache duration becomes zero and an oversized duration is capped at the supported millisecond range. Registering exporter options does not add an ASP.NET Core route. The diagnostic provider owns the independent listener and releases it on disposal.

The default addresses are loopback addresses. For a reverse proxy in another container, configure a listener reachable from that container; the binding tool does not rewrite loopback addresses. The independent server owns its declared ports and cannot share a socket with another server. HTTPS uses Kestrel:Certificates from IConfiguration in the metrics service container, falling back to ApplicationContext.Current.Configuration. It does not inherit host Endpoints or HTTP.sys certificate bindings. The zongsoft.tools.binding instruction declares exposure intent and does not itself start a listener. Outbound `opentelemetry` exporters do not participate in this binding.

## Validation

[Unit tests](test/unit) check parameter validation, exporter option projection and binding declarations. They do not build the MeterProvider that starts the scrape server, open listening ports or send network requests. They still inherit the ASP.NET Core dependency from the Web library under test.

Run from the `Zongsoft.Diagnostics.Web` directory; build Core and Diagnostics first because Debug references their compiled assemblies:

```shell
dotnet build ../Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Diagnostics/src/Zongsoft.Diagnostics.csproj -p:GeneratePackageOnBuild=false
dotnet build test/unit/Zongsoft.Diagnostics.Web.Tests.csproj -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true
dotnet test test/unit/Zongsoft.Diagnostics.Web.Tests.csproj --no-build
```

[Integration tests](test/integration) exercise temporary Kestrel HTTP/HTTPS listeners, including domain and IP combinations, IPv4/IPv6 isolation, real metric output, certificate selection, disposal and startup-failure cleanup. Domain requests connect directly to loopback without DNS or hosts-file changes. They do not start a business host or replace reverse-proxy and production HTTPS deployment validation. Run them explicitly after building Core and Diagnostics:

```shell
dotnet build test/integration/Zongsoft.Diagnostics.Web.Tests.slnx -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true
dotnet test test/integration/Zongsoft.Diagnostics.Web.Tests.slnx --no-build
```

The normal `Zongsoft.Diagnostics.Web.slnx` solution and default Cake `test` task include only unit tests. The separate Cake command `dotnet cake build.cake --target=integration-test` builds and runs the integration project.


## Deployment and compatibility

Deploy both plugins to the following directories; the host supplies the foundational libraries:

```ini
[plugins zongsoft diagnostics]
nuget:Zongsoft.Diagnostics@1.13.0
[plugins zongsoft diagnostics web]
nuget:Zongsoft.Diagnostics.Web@1.0.0
```

These are the source versions introduced by the split; make the matching packages available before deployment. This change does not publish NuGet packages. `PrometheusExporterLauncher` keeps the `Zongsoft.Diagnostics.Telemetry` namespace but moves to the `Zongsoft.Diagnostics.Web` assembly. Direct consumers must reference the new package and rebuild. `driver="prometheus"` is unchanged.

The `Instrumentation.AspNetCore` dependency moved here; no automatic HTTP instrumentation was added.

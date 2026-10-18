# Web host integration tests

[简体中文](README.zh-Hans.md)

The xUnit suite exercises the public `Zongsoft.Web.WebApplicationBuilder` and `Application.Web` entry points. Endpoint scenarios and Web host construction run in separate processes with their own temporary plugin directories. [WebIntegrationTests](WebIntegrationTests.cs) also verifies controller discovery, request principal/routes, descriptors and plugin diagnostics in isolated in-process scopes; assembly-level test parallelism is disabled. The fixture reuses the shipped `Main.plugin` registrations after removing workbench extensions, so no business workers or external services run.

The tests cover:

- Web host construction initializes the plugin context once, registers the controller activator and exposes plugin services through the host container without starting a listener.
- Without Kestrel:Endpoints declarations, native builder behavior is preserved: other conflicting plugin keys remain allowed. urls, http_ports, app.Urls and code Listen serve real HTTP requests; https_ports is validated at build time only, without a TLS handshake claim.
- Existing assembly scanning automatically registers endpoint integration for declared Web hosts. It consumes final plugin configuration and later protocol registration lazily, including imports, entry origins and plugin conflicts. Non-Web scans remain unaffected; unknown application protocols fail startup. Explicit AddWebEndpoints retains precedence, including a caller-selected section alongside default Kestrel declarations.
- Public Application.Web and WebApplicationBuilder entry points require no manual endpoint calls. Path declarations share physical listeners. Startup filters enforce scopes before initializers, static files and routing using actual loopback requests. Extra code listeners start, but requests without declared scopes are rejected; empty scopes reject all requests.
- Automatic monitoring remains singular even after repeated assembly scanning. Explicit registration keeps monitoring optional; duplicate explicit monitoring is idempotent: business changes stay quiet, network changes and new plugin conflicts warn once, listeners and scopes remain fixed. Stop and direct disposal release subscriptions; initialization failure disposes constructed services.
- Native TCP/multiplexed factories are not wrapped; selectors, cancellation, features and DI ownership remain intact. IPv6 TCP uses separate address families. Transport doubles do not validate real QUIC.

Debug builds consume local Core, Plugins and Web outputs. Build those source projects in that order, then build this solution and run the tests. The test project references only Plugins.Web as a project; Core, Plugins and Web are local assembly references through `HintPath`:

```powershell
dotnet build ../Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Plugins/src/Zongsoft.Plugins.csproj -p:GeneratePackageOnBuild=false
dotnet build ../Zongsoft.Web/src/Zongsoft.Web.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins.Web.slnx -p:GeneratePackageOnBuild=false -m:1
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net8.0
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net9.0
dotnet test ./test/Zongsoft.Plugins.Web.Tests.csproj --no-build -f net10.0
```

The tests are tagged `Category=Integration`. A subprocess must finish with exit code zero **and** write its completion marker; an early process exit cannot pass the test. Local HTTP tests reserve free ports immediately before use, have bounded request/process timeouts, and cancel workbench shutdown's process-exit behavior so HTTP shutdown can complete. They require IPv4 and IPv6 loopback socket support. No gRPC handler mounting, reverse proxy deployment or business host startup is included.

The request-context tests cover principal switching and route reads. Full Session dictionary consistency remains outside their coverage: ContainsKey requires keys in both local and route dictionaries, Count/enumeration omit local entries, and CopyTo increments before writing.

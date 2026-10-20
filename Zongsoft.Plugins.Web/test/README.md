# Web plugin tests

[English](README.md) | [简体中文](README.zh-Hans.md)

[Unit tests](unit/Zongsoft.Plugins.Web.Tests.csproj) verify controller discovery, request principal and route values, controller descriptors and plugin metadata with in-memory components. They do not construct a Web host, launch subprocesses or bind ports.

[Integration tests](integration/Zongsoft.Plugins.Web.Tests.csproj) verify the public `WebApplicationBuilder.BuildAsync` and `Application.WebAsync` entry points in isolated processes. They cover plugin configuration and service scanning, endpoint projection, path scopes, actual loopback HTTP requests, native transport selection, monitoring ownership, shutdown and asynchronous cleanup after initialization failure. The assembly is `Zongsoft.Plugins.Web.IntegrationTests`.

The main Plugins.Web solution and Cake `test` task include only unit tests. Integration tests have an explicit [solution](integration/Zongsoft.Plugins.Web.Tests.slnx) and Cake `integration-test` task. Both projects reference only Plugins.Web through `ProjectReference`; Core, Plugins and Web use local assembly `HintPath` references. Their shared [file fixture](.shared/PluginTestDirectory.cs) owns temporary plugin directories and reuses the shipped Main registrations.

## Running

Run from the framework root with the .NET 10 SDK and .NET 8, 9 and 10 runtimes. Build local Debug dependencies in order:

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/src/Zongsoft.Plugins.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Web/src/Zongsoft.Web.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins.Web/Zongsoft.Plugins.Web.slnx -p:GeneratePackageOnBuild=false
dotnet test ./Zongsoft.Plugins.Web/test/unit/Zongsoft.Plugins.Web.Tests.csproj --no-build --no-restore
```

Run integration tests explicitly:

```powershell
dotnet test ./Zongsoft.Plugins.Web/test/integration/Zongsoft.Plugins.Web.Tests.slnx -p:GeneratePackageOnBuild=false
```

Add `-f net8.0`, `-f net9.0` or `-f net10.0` to choose one framework. VSTest discovers the xUnit v3 tests. Integration subprocesses use `dotnet` from PATH, and require both exit code zero and a completion marker. Local HTTP tests use bounded request and process timeouts and require IPv4/IPv6 loopback socket support. They do not contact external services. Transport doubles do not establish real QUIC behavior, and HTTPS port projection does not establish a TLS handshake.

Assembly-level parallelism is disabled for process-wide application state. Fixtures await application and provider disposal before removing files. Initializer failure tests gate asynchronous disposal and verify the factory remains pending until cleanup finishes.

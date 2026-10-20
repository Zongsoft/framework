# Plugin tests

[English](README.md) | [简体中文](README.zh-Hans.md)

The tests exercise plugin composition and asynchronous ownership through public APIs. Both projects reference the Plugins source project and the built Core assembly through `HintPath`. Core runtime packages are declared explicitly. No production visibility is widened and no assertions depend on localized framework messages.

## Projects

- [Unit tests](unit/Zongsoft.Plugins.Tests.csproj) cover manifest loading and schema validation, tree navigation, builders and parsers, configuration, commands, workbench components, initializer ownership and asynchronous plugin disposal. They do not create Generic Host instances, subprocesses or network listeners.
- [Integration tests](integration/Zongsoft.Plugins.Tests.csproj) cover Generic Host configuration, Daemon/Terminal construction, service scanning, lifecycle ordering, worker rollback, failure aggregation, cancellation and initialization failure cleanup. Its assembly is `Zongsoft.Plugins.IntegrationTests`.
- The main Plugins solution and Cake `test` task include only unit tests. The [integration solution](integration/Zongsoft.Plugins.Tests.slnx) and Cake `integration-test` task are explicit entry points.

Both suites reuse the shipped [Main.plugin](../plugins/Main.plugin) registrations. Each fixture owns a temporary directory. Unit fixtures await the application context and service provider before removing their files; plugin trees release their cached entries through `DisposeAsync`.

## Running

Run from the framework root with the .NET 10 SDK and the .NET 8, 9 and 10 runtimes. Debug builds consume local dependency outputs; build Core first. These projects use VSTest and xUnit v3.

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/Zongsoft.Plugins.slnx -p:GeneratePackageOnBuild=false
dotnet test ./Zongsoft.Plugins/test/unit/Zongsoft.Plugins.Tests.csproj --no-build --no-restore
```

Run the integration suite explicitly:

```powershell
dotnet test ./Zongsoft.Plugins/test/integration/Zongsoft.Plugins.Tests.slnx -p:GeneratePackageOnBuild=false
```

Add `-f net8.0`, `-f net9.0` or `-f net10.0` to select one framework. Integration lifecycle tests invoke the matching test assembly with `dotnet` from PATH. The basic lifecycle probes use an in-memory terminal executor. Signal probes use the real console executor and standard-input reader: Windows creates an isolated hidden console, while Linux keeps a redirected pipe open. No listener or external service starts. A bounded timeout terminates only the test's child process, and a completion marker verifies that shutdown returned to the caller.

## Lifecycle assertions

[AsyncPluginDisposalTests](unit/AsyncPluginDisposalTests.cs) verifies asynchronous-only and dual-interface disposal, concurrent destroy calls, destruction reentry rejection, nested collections and plugins, error aggregation, cancelled waits, tree cache release and borrowed references. Disposal counts and reference identities distinguish plugin ownership from shared consumption.

[ApplicationLifecycleTests](unit/ApplicationLifecycleTests.cs) verifies workbench state transitions and initializer ownership. [HostWorkerLifecycleTests](integration/HostWorkerLifecycleTests.cs) uses explicit gates to prove that startup waits for every worker, rollback settles every attempt, and cancellation does not abandon cleanup. [TerminalLifecycleTests](integration/TerminalLifecycleTests.cs) verifies asynchronous input-loop lifetime. Timeouts guard against hangs; they do not establish event ordering.

[TerminalSignalTests](integration/TerminalSignalTests.cs) sends real OS signals inside isolated child processes. SIGINT at an idle prompt cancels the input wait and stops the host without waiting for the process-wide Console.In.ReadLine operation to finish. A handled SIGINT leaves standard input usable or returns from ReactiveAsync to the next command. SIGQUIT (Ctrl+Break on Windows) and Linux SIGTERM stop without a reactive veto, and a cleanup gate proves that repeated signals cannot bypass asynchronous shutdown. Blocked-interrupt cases verify that public host shutdown waits for the notification and preserves its failure; Linux additionally covers a real SIGQUIT overlapping the SIGINT notification. Other cases verify signal-subscriber failures, removed registrations and explicit IHostLifetime registrations. The supported environments are Windows and Linux; Windows requires console creation and Linux requires standard POSIX signals. Children retain their process handles for bounded cleanup; the tests do not locate processes again by saved PID.

Assembly-level test parallelism is disabled because application context, builder manager and plugin-tree caches contain process-wide state. Tests that verify concurrency start those calls explicitly.

## Coverage and style

```powershell
dotnet test ./Zongsoft.Plugins/test/unit/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore --collect "XPlat Code Coverage" --settings ./Zongsoft.Plugins/test/coverage.runsettings --results-directory ./Zongsoft.Plugins/test/TestResults
git -c core.whitespace=cr-at-eol diff --check -- Zongsoft.Plugins
```

[coverage.runsettings](coverage.runsettings) selects the Plugins assembly. Coverage reports are disposable outputs under Git-ignored `TestResults`. Source follows the [framework instructions](../../AGENTS.md) and [C# guidelines](../../../guidelines/zongsoft.csharp.guidelines.md).

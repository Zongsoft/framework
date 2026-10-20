# Core terminal input integration tests

[简体中文](README.zh-Hans.md)

This separate project exercises the process-wide `Terminal.Console.Input` through managed child processes and redirected standard-input pipes. It is excluded from the main Core solution and does not access external services.

Four pipe scenarios verify cancellation followed by process exit while the parent keeps stdin open, reuse of a pending line after cancellation and rejection of overlapping waiters, singleton identity with no-op Dispose/Close and explicit rejection of non-line reads, and Unicode, blank lines, final unterminated lines, and EOF. Child processes have a bounded execution time and are owned through their `Process` handles. No native API, Python, PTY, or temporary input file is required.

Two controlled TextReader scenarios use Console.SetIn in isolated child processes. Explicit gates prove that repeated waiter cancellation keeps one underlying ReadLine, late results and failures remain available, and synchronous, parameterless asynchronous, and cancellation-aware line reads share the same state. EOF and failures do not start additional reads.

Run from this directory with the required SDKs installed:

```powershell
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net8.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net9.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.IntegrationTests.csproj --framework net10.0 -p:GeneratePackageOnBuild=false
```

When running a previously built test assembly with a standalone Runtime, set `DOTNET_HOST_PATH` to the absolute path of its `dotnet` executable. The child processes reuse that Runtime.

Physical keyboard editing and host signal policy are outside this suite's scope.

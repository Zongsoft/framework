# Core terminal integration tests

[简体中文](README.zh-Hans.md)

This separate project exercises the process-wide `Terminal.Console.Input` through managed child processes and redirected standard-input pipes. It is excluded from the main Core solution and does not access external services.

The suite covers line-input cancellation, result and failure retention, overlapping-reader rejection, EOF, and process shutdown. Windows additionally covers Shell command output, exit codes, timeout and cancellation. It requires no external services.

Run from this directory with the required SDKs installed:

```powershell
dotnet test Zongsoft.Core.Tests.csproj --framework net8.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.Tests.csproj --framework net9.0 -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Core.Tests.csproj --framework net10.0 -p:GeneratePackageOnBuild=false
```

When running a previously built test assembly with a standalone Runtime, set `DOTNET_HOST_PATH` to the absolute path of its `dotnet` executable. The child processes reuse that Runtime.

Physical keyboard editing and host signal policy are outside this suite's scope.

# Zongsoft.Externals.Hangfire.Samples Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Samples)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Samples)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This project is the minimal handler sample for the [Zongsoft Hangfire integration](..). It defines `MyHandler`, derives it from `HandlerBase<object>`, and registers it under `/Workbench/Scheduler/Handlers` with the stable name `MyHandler`.

When Hangfire dispatches a job to that name, the handler writes the argument, parameters, and an incrementing execution count to the Zongsoft diagnostics log. Use the sample together with the core Hangfire plugin, a configured storage plugin, and a running Hangfire server.

The sample demonstrates handler registration only. Scheduling recurring and delayed jobs, server configuration, and storage setup are described in the [parent documentation](..).


## Build, Load, and Verify

```shell
dotnet build externals/hangfire/samples/Zongsoft.Externals.Hangfire.Samples.csproj
```

Deploy the sample assembly and plugin after the core Hangfire plugin. Schedule the stable handler name `MyHandler` through the parent integration, start a Hangfire server, and confirm a debug log containing `Count`, `Argument`, and `Parameters`.

> 💡 The counter is process-local demonstration state. Restarting the host resets it, and multiple workers do not share one count.

🚨 Use disposable storage and harmless arguments. Hangfire retries failed jobs by policy, so real handlers must be idempotent and must not assume one execution.

Stop the server and remove the sample plugin to clean up; delete test jobs with the storage's supported administration workflow.

# Zongsoft.Externals.Hangfire Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Hangfire` adapts [Hangfire](https://www.hangfire.io/) to the Zongsoft scheduling abstractions. It provides:

- `IScheduler<TriggerOptions.Cron>` for recurring jobs driven by Cron expressions.
- `IScheduler<TriggerOptions.Latency>` for one-off jobs executed after a delay.
- A background `Server` worker that dispatches jobs to Zongsoft `IHandler` instances.
- Optional Redis storage and ASP.NET Core Dashboard integration.

The library targets .NET 8, .NET 9, and .NET 10. Hangfire storage must be configured before the scheduler or server starts.

## Packages

| Package | Purpose |
| --- | --- |
| `Zongsoft.Externals.Hangfire` | Core scheduler and background server integration. |
| `Zongsoft.Externals.Hangfire.Storages.Redis` | Hangfire storage backed by `Zongsoft.Externals.Redis`. |
| `Zongsoft.Externals.Hangfire.Web` | Registers Hangfire services and maps the Dashboard in a Zongsoft web application. |

Install the packages required by the host:

```shell
dotnet add package Zongsoft.Externals.Hangfire
dotnet add package Zongsoft.Externals.Hangfire.Storages.Redis
dotnet add package Zongsoft.Externals.Hangfire.Web
```

## Plugin setup

Load `Zongsoft.Externals.Hangfire.plugin` to register the schedulers. A daemon host should also load `Zongsoft.Externals.Hangfire-daemon.plugin`; it creates and starts the background server and exposes `/Workbench/Scheduler/Handlers` as the handler registration point.

Choose and configure exactly one Hangfire `JobStorage`. The Redis adapter is available through `Zongsoft.Externals.Hangfire.Storages.Redis.plugin` and uses the `Hangfire` connection setting from the Zongsoft Redis settings provider.

## Server configuration

The server reads options from `Externals/Hangfire/Server`. The packaged defaults set the schedule polling interval to 10 seconds:

```xml
<options>
	<option path="Externals/Hangfire">
		<server scheduleInterval="10s" />
	</option>
</options>
```

Supported server attributes:

| Attribute | Description |
| --- | --- |
| `queues` | Queue names processed by this server. |
| `workerCount` | Number of concurrent Hangfire workers. |
| `stopTimeout` | Grace period when stopping the server. |
| `shutdownTimeout` | Maximum server shutdown duration. |
| `scheduleInterval` | Polling interval for scheduled jobs. |
| `heartbeatInterval` | Interval between server heartbeats. |
| `checkInterval` | Interval used to check inactive servers. |
| `serverTimeout` | Time after which a server is considered inactive. |

Only positive numeric or duration values override Hangfire defaults. When the worker is named something other than `Server`, its Hangfire server name is emitted as `<worker-name>@<machine-name>`.

## Register a handler

Jobs are addressed by handler name. Implement `IHandler` (usually by deriving from `HandlerBase<TArgument>`) and register the instance under `/Workbench/Scheduler/Handlers`:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;

public sealed class ReportHandler : HandlerBase<int>
{
	protected override ValueTask OnHandleAsync(
		int reportId,
		Parameters parameters,
		CancellationToken cancellation)
	{
		Console.WriteLine($"Generating report {reportId}.");
		return ValueTask.CompletedTask;
	}
}
```

```xml
<extension path="/Workbench/Scheduler/Handlers">
	<object name="Report" type="Example.ReportHandler, Example" />
</extension>
```

Every running Hangfire `Server` whose handler collection contains `Report` receives the dispatched job. Keep handler names stable because persisted Hangfire jobs store that name.

## Schedule jobs

Resolve or inject the typed scheduler that matches the trigger mode:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Scheduling;

public sealed class ReportScheduler(IScheduler<TriggerOptions.Cron> cron,
	IScheduler<TriggerOptions.Latency> latency)
{
	public ValueTask<string> ScheduleDailyAsync(CancellationToken cancellation = default) =>
		cron.ScheduleAsync(
			"Report",
			42,
			new TriggerOptions.Cron("daily-report", "0 2 * * *", TimeZoneInfo.Utc),
			cancellation);

	public ValueTask<string> ScheduleOnceAsync(CancellationToken cancellation = default) =>
		latency.ScheduleAsync(
			"Report",
			42,
			new TriggerOptions.Latency(TimeSpan.FromMinutes(5)),
			cancellation);
}
```

The returned string is the Hangfire job identifier. Use `RescheduleAsync(identifier)` to trigger it again and `UnscheduleAsync(identifier)` to remove it.

## Command integration

The generic `Scheduler`, `Schedule`, `Reschedule`, and `Unschedule` commands are provided by the `Zongsoft.Commands` package under `Zongsoft.Scheduling.Commands`. Load `Zongsoft.Commands.plugin` when command-line scheduling is required; the Hangfire package itself no longer owns those commands.

## Web Dashboard

Loading `Zongsoft.Externals.Hangfire.Web.plugin` registers Hangfire with the ASP.NET Core host and calls `UseHangfireDashboard()` during application initialization. Configure Dashboard authorization and routing according to the security requirements of the hosting application; do not expose the Dashboard publicly without access controls.

## Samples

See the [sample project](samples) for a minimal handler plugin. Storage-specific and web-specific projects are available under [storages](storages) and [web](web).

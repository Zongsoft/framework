# Zongsoft.Externals.Hangfire 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Hangfire` 将 [Hangfire](https://www.hangfire.io/) 适配到 Zongsoft 调度抽象，主要提供：

- 基于 Cron 表达式的周期作业调度器 `IScheduler<TriggerOptions.Cron>`；
- 指定延迟后执行一次的作业调度器 `IScheduler<TriggerOptions.Latency>`；
- 将作业分派给 Zongsoft `IHandler` 实例的后台 `Server` 工作者；
- 可选的 Redis 存储和 ASP.NET Core Dashboard 集成。

本库支持 .NET 8、.NET 9 和 .NET 10。调度器或服务器启动前必须先配置 Hangfire 存储。

## 程序包

| 程序包 | 用途 |
| --- | --- |
| `Zongsoft.Externals.Hangfire` | 核心调度器和后台服务器集成。 |
| `Zongsoft.Externals.Hangfire.Storages.Redis` | 基于 `Zongsoft.Externals.Redis` 的 Hangfire 存储。 |
| `Zongsoft.Externals.Hangfire.Web` | 在 Zongsoft Web 应用中注册 Hangfire 服务并映射 Dashboard。 |

按宿主需要安装相应程序包：

```shell
dotnet add package Zongsoft.Externals.Hangfire
dotnet add package Zongsoft.Externals.Hangfire.Storages.Redis
dotnet add package Zongsoft.Externals.Hangfire.Web
```

## 插件设置

加载 `Zongsoft.Externals.Hangfire.plugin` 以注册调度器。守护进程宿主还应加载 `Zongsoft.Externals.Hangfire-daemon.plugin`，该插件会创建并启动后台服务器，同时将 `/Workbench/Scheduler/Handlers` 作为处理器注册入口。

请选择并配置一个 Hangfire `JobStorage`。Redis 适配器由 `Zongsoft.Externals.Hangfire.Storages.Redis.plugin` 提供，它使用 Zongsoft Redis 设置提供程序中名为 `Hangfire` 的连接设置。

## 服务器配置

服务器从 `Externals/Hangfire/Server` 读取设置。程序包内置设置将计划作业的轮询间隔设为 10 秒：

```xml
<options>
	<option path="Externals/Hangfire">
		<server scheduleInterval="10s" />
	</option>
</options>
```

支持以下服务器属性：

| 属性 | 说明 |
| --- | --- |
| `queues` | 本服务器处理的队列名称。 |
| `workerCount` | Hangfire 并发工作线程数。 |
| `stopTimeout` | 停止服务器时的宽限时间。 |
| `shutdownTimeout` | 服务器关闭的最大等待时间。 |
| `scheduleInterval` | 计划作业的轮询间隔。 |
| `heartbeatInterval` | 服务器心跳间隔。 |
| `checkInterval` | 检查失活服务器的间隔。 |
| `serverTimeout` | 将服务器判定为失活的超时时间。 |

只有正数或正时长才会覆盖 Hangfire 默认值。当工作者名称不是 `Server` 时，对应的 Hangfire 服务器名称为 `<工作者名称>@<机器名称>`。

## 注册处理器

作业通过处理器名称寻址。实现 `IHandler`（通常继承 `HandlerBase<TArgument>`），并将实例注册到 `/Workbench/Scheduler/Handlers`：

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

处理器集合中包含 `Report` 的每个运行中 Hangfire `Server` 都会收到分派的作业。由于持久化的 Hangfire 作业会保存处理器名称，因此应保持名称稳定。

## 调度作业

解析或注入与触发模式对应的强类型调度器：

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

返回的字符串是 Hangfire 作业标识。可通过 `RescheduleAsync(identifier)` 再次触发作业，通过 `UnscheduleAsync(identifier)` 删除作业。

## 命令集成

通用的 `Scheduler`、`Schedule`、`Reschedule` 和 `Unschedule` 命令由 `Zongsoft.Commands` 程序包提供，位于 `Zongsoft.Scheduling.Commands` 命名空间。需要通过命令行管理调度时请加载 `Zongsoft.Commands.plugin`；Hangfire 程序包不再包含这些命令。

## Web Dashboard

加载 `Zongsoft.Externals.Hangfire.Web.plugin` 后，会在 ASP.NET Core 宿主中注册 Hangfire，并在应用初始化阶段调用 `UseHangfireDashboard()`。请根据宿主应用的安全要求配置 Dashboard 的授权和路由，切勿在没有访问控制的情况下将其暴露到公网。

## 示例

最小处理器插件请参阅[示例项目](samples)。存储和 Web 相关项目分别位于 [storages](storages) 与 [web](web) 目录。

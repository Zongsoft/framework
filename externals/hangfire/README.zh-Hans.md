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

真实的[样例插件](samples/Zongsoft.Externals.Hangfire.Samples.plugin)注册 [MyHandler](samples/MyHandler.cs)。它继承 `HandlerBase<object>`，记录参数、附加参数及进程内调用次数：

```xml
<extension path="/Workbench/Scheduler/Handlers">
	<object name="MyHandler" type="Zongsoft.Externals.Hangfire.Samples.MyHandler, Zongsoft.Externals.Hangfire.Samples" />
</extension>
```

部署样例程序集与完整清单，不要只复制此扩展片段。处理器集合包含该名称的各运行中 Server 都会接收分派的作业。作业会持久化名称，改名可能让已有任务失去处理器。这个样例不生成业务报表。

## 调度作业

以下 API 改写通过公共调度契约调度已有的 `MyHandler` 样例。在 daemon、存储和样例插件装配完成后，使用隔离测试存储：

```csharp
using Zongsoft.Services;
using Zongsoft.Scheduling;

var services = ApplicationContext.Current.Services;
var cron = services.ResolveRequired<IScheduler<TriggerOptions.Cron>>();
var latency = services.ResolveRequired<IScheduler<TriggerOptions.Latency>>();

var recurringId = await cron.ScheduleAsync(
	"MyHandler", "Zongsoft.Externals.Hangfire.Samples",
	new TriggerOptions.Cron("hangfire-samples", "0 2 * * *", TimeZoneInfo.Utc),
	CancellationToken.None);

var delayedId = await latency.ScheduleAsync(
	"MyHandler", "Zongsoft.Externals.Hangfire.Samples",
	new TriggerOptions.Latency(TimeSpan.FromMinutes(5)),
	CancellationToken.None);
```

两种触发模式可按需选择，不必同时创建。返回值为作业标识，请保留以便 `RescheduleAsync` 或 `UnscheduleAsync`，测试后的周期作业需显式清理。框架真实的 [ScheduleCommand](../../Zongsoft.Commands/src/Scheduling/ScheduleCommand.cs)同样将处理器名、管道值和触发选项传给 `IScheduler.ScheduleAsync`，不需要另造 ReportScheduler 包装类。

## 命令集成

通用的 `Scheduler`、`Schedule`、`Reschedule` 和 `Unschedule` 命令由 `Zongsoft.Commands` 程序包提供，位于 `Zongsoft.Scheduling.Commands` 命名空间。需要通过命令行管理调度时请加载 `Zongsoft.Commands.plugin`；Hangfire 程序包不再包含这些命令。

## Web Dashboard

加载 `Zongsoft.Externals.Hangfire.Web.plugin` 后，会在 ASP.NET Core 宿主中注册 Hangfire，并在应用初始化阶段调用 `UseHangfireDashboard()`。请根据宿主应用的安全要求配置 Dashboard 的授权和路由，切勿在没有访问控制的情况下将其暴露到公网。

## 示例

最小处理器插件请参阅[示例项目](samples)。存储和 Web 相关项目分别位于 [storages](storages) 与 [web](web) 目录。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单按 `Zongsoft.Externals.Hangfire-$(site).plugin` 选择附加插件，daemon 变体贡献调度器及启动工作器。启用前部署 Redis 等存储适配并完成配置，Web 仪表盘是独立包，不等于工作器宿主。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Hangfire.Daemon` | [Zongsoft.Externals.Hangfire-daemon.plugin](src/Zongsoft.Externals.Hangfire-daemon.plugin) |
| `Zongsoft.Externals.Hangfire` | [Zongsoft.Externals.Hangfire.plugin](src/Zongsoft.Externals.Hangfire.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Hangfire.deploy](src/Zongsoft.Externals.Hangfire.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals hangfire]
nuget:Zongsoft.Externals.Hangfire
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Hangfire.option`、`Zongsoft.Externals.Hangfire.plugin`、`Zongsoft.Externals.Hangfire-$(site).plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

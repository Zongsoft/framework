# Zongsoft.Messaging.ZeroMQ 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.ZeroMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

<a name="abstract"></a>
## 概述

Zongsoft.Messaging.ZeroMQ 是基于 [NetMQ](https://github.com/zeromq/netmq) 的消息队列适配器，用于对接 [Zongsoft.Core](../../Zongsoft.Core) 中的消息和通信抽象。它通过 `IMessageQueue` 提供主题发布与订阅，并提供请求响应和事件通道适配器。

本项目包含一个轻量的 XPUB/XSUB 消息交换服务 `ZeroQueueServer`。客户端先查询其发现端点，再连接返回的发布和订阅数据端点。交换服务不保存状态，适合低延迟的瞬态消息分发，不属于持久化消息队列。

<a name="features"></a>
## 功能特性

- 实现 Zongsoft 的 `IMessageQueue`、`IRequester`、`IResponder` 和 `IEventChannel` 抽象；
- 通过 XPUB/XSUB 交换服务支持多个发布者和订阅者；
- 支持主题前缀、可选消息分组、实例过滤和心跳；
- 支持超过指定阈值后使用 Brotli 压缩消息载荷；
- 支持独立运行以及 Zongsoft 插件化宿主；
- 支持 .NET 8、.NET 9 和 .NET 10。

<a name="installation"></a>
## 安装

安装 NuGet 包：

```shell
dotnet add package Zongsoft.Messaging.ZeroMQ
```

从本仓库构建时，应先构建 Zongsoft.Core：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj
dotnet build messaging/zero/Zongsoft.Messaging.ZeroMQ.slnx
```

<a name="topology"></a>
## 交换拓扑

| 端点 | 随包配置的默认值 | 用途 |
| --- | :---: | --- |
| 发现端点 | `7969` | 客户端查询两个数据端点的端口。 |
| 发布者入口 | `32101` | 应用发布者连接此端点。 |
| 订阅者出口 | `32102` | 应用订阅者连接此端点。 |

`7969` 是内置的发现端口默认值。两个数据端口均可配置；未指定时，`ZeroQueueServer` 会绑定随机端口并通过发现端点返回。部署时建议固定数据端口，以便交换服务重启后现有客户端能重新连接原端点。

服务器会在所有网络接口上绑定 TCP 端点，且不会配置身份认证或传输加密。跨不可信网络使用前，请通过主机或网络边界限制访问，或另行增加经过认证的安全传输层。

<a name="configuration"></a>
## 配置

### 服务端

随包提供的守护进程插件会自动启动 `ZeroQueueServer`。在 `/Messaging/ZeroMQ/Servers` 下配置数据端点：

```xml
<configuration>
	<option path="/Messaging/ZeroMQ">
		<servers port="32101,32102">
			<server server.name="unnamed" port="*" />
		</servers>
	</option>
</configuration>
```

默认服务器或没有匹配到命名服务器条目时使用集合级 `port`；匹配到的 `<server>` 条目使用自己的端口对。第一个数字是发布者入口，第二个数字是订阅者出口；`*` 表示随机选择数据端口。

独立应用可以直接启动交换服务：

```csharp
using var server = new ZeroQueueServer();
await server.StartAsync(["--incoming:32101", "--outgoing:32102"]);
```

### 客户端连接

在 `/Messaging/ConnectionSettings` 下定义 `ZeroMQ` 连接：

```xml
<configuration>
	<option path="/Messaging">
		<connectionSettings default="ZeroMQ">
			<connectionSetting connectionSetting.name="ZeroMQ"
			                   driver="ZeroMQ"
			                   value="server=127.0.0.1;port=7969;group=Demo;client=MyApplication;" />
		</connectionSettings>
	</option>
</configuration>
```

| 设置项 | 默认值 | 说明 |
| --- | --- | --- |
| `Server` | 必填 | 发现端点的主机名或 IP 地址，不要包含 `tcp://`。 |
| `Port` | `7969` | 发现端点端口。 |
| `Topic` | 空 | `ProduceAsync` 或 `SubscribeAsync` 未指定主题时使用的主题。 |
| `Group` | 空 | 按 `Group:Topic` 形式添加前缀，用于隔离共享交换服务的应用。 |
| `Client` | 空 | 自动生成实例标识时使用的稳定客户端名称。 |
| `Instance` | 自动生成 | 明确指定生产者实例标识；为空或 `*` 时生成唯一标识。 |
| `Filter` | 排除自身 | 以逗号分隔的实例过滤表达式，用于控制接收哪些生产者的消息。 |
| `Timeout` | `10s` | 发现和订阅同步的超时时长。 |
| `Heartbeat` | `10s` | 心跳间隔；小于等于零时禁用心跳。 |

默认过滤规则会排除当前队列实例自己发布的消息。`Filter=*` 接受所有实例；`Filter=.`（或 `~`）仅接受当前实例；普通实例标识组成允许列表，`!identifier` 表示排除指定实例。

<a name="usage"></a>
## 使用

### 发布与订阅

不使用 Zongsoft 插件容器时，可以直接创建队列：

```csharp
using System.Text;
using Zongsoft.Messaging.ZeroMQ;
using Zongsoft.Messaging.ZeroMQ.Configuration;

var settings = ZeroConnectionSettingsDriver.Instance.GetSettings(
	"ZeroMQ",
	"server=127.0.0.1;port=7969;group=Demo;client=Sample;");

using var queue = new ZeroQueue("ZeroMQ", settings);

var consumer = await queue.SubscribeAsync("orders/created", message =>
	Console.WriteLine(Encoding.UTF8.GetString(message.Data.Span)));

await queue.ProduceAsync("orders/created", "订单 #1001".AsMemory());

await consumer.UnsubscribeAsync();
```

插件化宿主可从名为 `ZeroMQ` 的 `IMessageQueueProvider` 中获取命名队列，然后使用相同的 `ProduceAsync` 和 `SubscribeAsync` API。

主题订阅采用前缀匹配。一个 `ZeroQueue` 对每个有效主题只保留一个消费者；再次订阅同一个有效主题会返回已有消费者，不会替换处理器或选项。设置 `Group=Demo` 后，有效主题及处理器收到的 `Message.Topic` 都会包含前缀，例如 `Demo:orders/created`。

### 压缩

通过 `Compressive` 属性指定启用 Brotli 压缩的最小消息字节数：

```csharp
var options = new MessageEnqueueOptions();
options.Properties["Compressive"] = 4 * 1024;

await queue.ProduceAsync("documents/updated", payload, options);
```

压缩是当前适配器唯一实现的 `MessageEnqueueOptions` 行为。

| 消息选项 | 支持情况 |
| --- | --- |
| `Properties["Compressive"]` | 支持；超过指定字节阈值后启用 Brotli。 |
| 标签 | ZeroMQ 适配器未使用。 |
| 延迟和过期时间 | 未实现。 |
| 优先级 | 未实现。 |
| 可靠性 | 传输仍是瞬态、尽力而为的 PUB/SUB。 |
| 订阅可靠性和失败策略 | 当前处理器调度器未实现。 |

### 请求与响应

`ZeroRequester` 和 `ZeroResponder` 将队列主题适配到 Zongsoft 通信接口。请求发布到 URL 主题，响应默认使用 `<url>/reply` 主题：

```csharp
var requester = new ZeroRequester { Queue = queue };
var token = await requester.RequestAsync("services/ping", "Ping"u8.ToArray());

foreach(var response in token.GetResponses(TimeSpan.FromSeconds(3)))
	Console.WriteLine(Encoding.UTF8.GetString(response.Data.Span));
```

响应器会订阅已注册处理器所公开的 URL：

```csharp
var responder = new ZeroResponder { Queue = queue };
responder.Handlers.Add(new PingHandler());
await responder.StartAsync([]);
```

处理器接收 `IRequest`，并可通过传入的 `IResponder` 返回数据。完整的处理器范例参见[请求器测试](test/ZeroRequesterTests.cs)和[响应器测试](test/ZeroResponderTests.cs)。

### 事件通道

`ZeroQueueEventChannel` 通过 `Events/...` 主题连接 `EventExchanger` 与消息队列：

```csharp
await using var channel = new ZeroQueueEventChannel(queue);
await channel.OpenAsync(exchanger);
await channel.SendAsync(eventContext);
```

插件清单会为宿主应用自动注册该通道。当前版本中，请让请求响应及事件通道使用的队列保持 `Group` 为空；这些适配器尚未规范化带分组前缀的物理主题。

<a name="semantics"></a>
## 投递语义

本适配器遵循 ZeroMQ PUB/SUB 行为：

- 消息是瞬态的，`ZeroQueueServer` 不会持久化消息；
- 不提供 Broker 确认、消费者确认、重试、去重或重放；
- 对端正在连接或重连、匹配订阅尚未传播、或者套接字达到高水位时，消息都可能被丢弃；
- `ProduceAsync` 在消息进入队列的本地发送路径后完成，不代表订阅者已经收到或处理；
- `SubscribeAsync` 会同步订阅端连接，但不会与发布端建立端到端投递确认；
- 当前版本应避免发送空载荷。

如果业务不能接受消息丢失，应选择持久化消息 Broker，或者增加应用层确认协议。

<a name="samples"></a>
## 范例与排障

[.NET 10 范例](samples)包含交互式交换服务器和客户端。先启动服务器，再分别启动订阅客户端和发布客户端。具体命令参见[范例指南](samples/README.zh-Hans.md)。

无法收到消息时：

1. 确认发现端口和返回的两个数据端口在所需方向上均可访问；
2. 确认发布者与订阅者使用相同的 `Group` 和兼容的主题前缀；
3. 检查 `Filter` 设置——默认不会接收本实例发布的消息；
4. 先建立订阅再发布，并为 PUB/SUB 订阅传播预留时间；
5. 服务端重启时保持数据端口不变，或者重建队列以重新执行端点发现。

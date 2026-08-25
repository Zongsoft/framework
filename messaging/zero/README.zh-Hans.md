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

本项目包含 `ZeroQueueServer`：它以 XPUB/XSUB 处理最多一次消息，并以持久化确认通道处理至少一次消息。客户端会自动发现当前 Broker 代次及运行端点。

<a name="features"></a>
## 功能特性

- 实现 Zongsoft 的 `IMessageQueue`、`IRequester`、`IResponder` 和 `IEventChannel` 抽象；
- 通过 XPUB/XSUB 交换服务支持多个发布者和订阅者；
- 支持主题前缀、可选消息分组、实例过滤和心跳；
- 支持超过指定阈值后使用 Brotli 压缩消息载荷；
- 支持即时最多一次广播，以及由 Broker 持久接纳、显式确认和竞争消费的至少一次投递；
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
| 发现端点 | `7969` | 客户端查询 Broker 代次和运行端点。 |
| 可靠性控制端点 | `32100` | 可靠订阅登记、投递和确认。 |
| 发布者进站端点 | `32101` | 应用发布者连接此端点。 |
| 订阅者出站端点 | `32102` | 应用订阅者连接此端点。 |

`7969` 是内置发现端口。运行端点未配置时会随机选择，Broker 重启后客户端会重新发现。生产环境仍建议固定三个运行端口，以简化防火墙与运维配置。

服务器会在所有网络接口上绑定 TCP 端点，且不会配置身份认证或传输加密。跨不可信网络使用前，请通过主机或网络边界限制访问，或另行增加经过认证的安全传输层。

<a name="configuration"></a>
## 配置

### 服务端

随包提供的守护进程插件会自动启动 `ZeroQueueServer`。在 `/Messaging/ZeroMQ/Servers` 下配置数据端点：

```xml
<configuration>
	<option path="/Messaging/ZeroMQ">
		<servers port="32100,32101,32102">
			<server server.name="unnamed" port="*" />
		</servers>
	</option>
</configuration>
```

三个数字依次表示可靠性控制、发布者进站和订阅者出站端口。两段式配置仍表示 `Incoming,Outgoing`，此时 Control 配置值为零，Server 挂载 Storage 后会随机绑定 Control 端口；仅当 Server 已挂载 `IMessageStorage` 时才会启动 Control 端点。`*` 表示运行端点使用随机端口。

独立应用可以直接启动交换服务：

```csharp
using var server = new ZeroQueueServer();
server.Storage = ResolveMessageStorage(); // 由独立存储插件提供；仅 LeastOnce 需要。
await server.StartAsync(["--control:32100", "--incoming:32101", "--outgoing:32102"]);
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
| `ReconnectInterval` | `1s` | 重新发现端点的最小间隔。 |

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

var identifier = await queue.ProduceAsync("orders/created", "订单 #1001".AsMemory());
if(identifier == null)
	Console.WriteLine("发送瞬间没有可见的匹配订阅，消息未发送。");

await consumer.UnsubscribeAsync();
```

插件化宿主可从名为 `ZeroMQ` 的 `IMessageQueueProvider` 中获取命名队列，然后使用相同的 `ProduceAsync` 和 `SubscribeAsync` API。

主题订阅采用前缀匹配。一个 `ZeroQueue` 对每个逻辑主题只保留一个消费者；再次订阅同一个主题会返回已有消费者，不会替换处理器或选项。设置 `Group=Demo` 后，网络上的物理主题为 `Demo:orders/created`，处理器收到的 `Message.Topic` 仍为逻辑主题 `orders/created`。

同一订阅内的处理器按接收顺序串行执行。待处理队列达到容量后，该订阅会暂停从 Poller 接收，消费腾出空间后再恢复；背压只作用于相应订阅，不会阻塞其他 Socket。

### 压缩

通过 `MessageEnqueueOptions.Compression` 指定启用 Brotli 压缩的最小消息字节数，非正数表示不压缩：

```csharp
var options = new MessageEnqueueOptions() { Compression = 4 * 1024 };

await queue.ProduceAsync("documents/updated", payload, options);
```

### 至少一次投递

订阅和发布都要显式选择 `LeastOnce`。只有 Broker 必须挂载 `IMessageStorage`；发布端不存储消息。处理器正常返回不代表确认，必须调用 `AcknowledgeAsync`。

```csharp
var subscriptionOptions = new MessageSubscribeOptions(MessageReliability.LeastOnce);
var enqueueOptions = new MessageEnqueueOptions(MessageReliability.LeastOnce)
{
	Expiration = TimeSpan.FromMinutes(5),
};

server.Storage = ResolveMessageStorage(); // 由独立存储插件提供

var consumer = await queue.SubscribeAsync("orders/created", new ReliableOrderHandler(), subscriptionOptions);

var identifier = await queue.ProduceAsync("orders/created", payload, enqueueOptions);

sealed class ReliableOrderHandler : HandlerBase<Message>
{
	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await SaveOrderAsync(message.Data, cancellation);
		await message.AcknowledgeAsync(cancellation);
	}
}
```

Broker 只在发送瞬间存在在线匹配订阅时接纳消息：没有订阅返回 `null` 且不写入 Storage；存在订阅则先持久化 Pending，再返回消息标识。之后按主题在在线订阅者间竞争投递，任一消费者确认即删除 Pending。未确认会沿用同一 `Message.Identifier` 重投，也可能改投另一个消费者，因此处理器必须保证业务幂等。

`ZeroQueueServer.Storage` 只能在 Server 停止时赋值。外部 Storage 的生命周期由插件容器管理，Server 不会释放它。Broker 未配置 Storage 时仍提供 `MostOnce` Broadcast，但不启动可靠 Control 端点，发现响应返回 `Control:0`；此时 `LeastOnce` 操作会失败。

| 消息选项 | 支持情况 |
| --- | --- |
| `Compression` | `MostOnce` 支持；超过指定字节阈值后启用 Brotli，可靠 Control 通道不压缩。 |
| 标签 | `LeastOnce` 保留并传递；`MostOnce` 双帧格式暂不传递。 |
| `Delay` | 不支持；正数请求由 Core 根据 `Features` 在进入驱动前拒绝。 |
| 过期时间 | `LeastOnce` 支持；零表示永不过期。 |
| 优先级 | 未实现。 |
| `MostOnce` | 支持；发送瞬间无匹配订阅返回 `null`，否则本地发送一次。 |
| `LeastOnce` | 支持 Broker 持久接纳、竞争消费、显式确认和同标识重投。 |
| `ExactlyOnce` | 不支持，并在创建传输状态前失败。 |
| 订阅失败策略 | 当前处理器调度器未实现。 |

### 请求与响应

`ZeroRequester` 和 `ZeroResponder` 将队列主题适配到 Zongsoft 通信接口。请求发布到 URL 主题，响应默认使用 `<url>/reply` 主题：

```csharp
await using var requester = new ZeroRequester { Queue = queue };
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

插件清单会为宿主应用自动注册该通道。请求响应和事件通道支持 `Group`；分组前缀只在网络边界添加，适配器始终使用逻辑主题。

<a name="semantics"></a>
## 投递语义

投递契约由 `MessageReliability` 决定：

- `MostOnce` 是瞬态广播。发送瞬间 XPUB 未观察到匹配订阅时返回 `null` 且不发送；否则本地发送一次并返回唯一标识。多个广播订阅者收到同一标识，但非空标识不表示远端或 Handler 已收到。
- `LeastOnce` 在没有在线匹配订阅时返回 `null` 且不持久化；Broker 先持久化 Pending 后立即向发布者返回唯一标识，不等待 Handler 确认。
- Broker 在在线订阅者间竞争投递；未确认时使用同一标识重投，任一有效确认即删除 Pending。全部订阅者离线后，已接纳消息继续保留，待订阅恢复后再投递。
- `LeastOnce` 允许处理器重复执行，不会对业务副作用自动去重，也不提供精确一次。
- Control 超时、调用取消或断线可能导致发布者无法判断 Broker 是否已经接纳；这些操作只停止本地等待，不能撤销已经开始的 Broker 接纳，业务重试仍可能产生重复。
- 可靠消息过期后从 Broker Pending 删除并记录诊断。
- Queue 在构造时快照连接、端口、分组、过滤、超时和心跳设置；运行中修改原设置对象不会改变既有连接；
- 支持空业务载荷。

消息存储是独立插件，不属于 ZeroMQ 驱动。本包不提供默认文件存储；需要 `LeastOnce` 时，应用必须为每个 Broker Server 赋予独立的 `IMessageStorage` 实例。`Name` 表示存储实现名称，`Settings` 决定该实例的连接和数据作用域，实例生命周期由插件容器管理。选择实现时应确认它能在 `SetAsync` 返回前持有消息快照，并满足所需的进程重启耐久性。

<a name="samples"></a>
## 范例与排障

[.NET 10 范例](samples)包含交互式交换服务器和客户端。先启动服务器，再分别启动订阅客户端和发布客户端。具体命令参见[范例指南](samples/README.zh-Hans.md)。

无法收到消息时：

1. 确认发现、可靠性控制、发布者进站和订阅者出站端口均可访问；
2. 确认发布者与订阅者使用相同的 `Group` 和兼容的主题前缀；
3. 检查 `Filter` 设置——默认不会接收本实例发布的消息；
4. `ProduceAsync` 返回 `null` 时，确认发布瞬间 Broker 已能看到匹配订阅；业务可按自身策略决定是否重试；
5. 使用 `LeastOnce` 时检查 Server 的 `Storage`、Control 端口、显式确认和过期时间；
6. 可靠投递长期未完成时检查 Broker 的 Pending 数据、在线订阅和消费者幂等处理。

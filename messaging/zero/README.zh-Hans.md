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
- 支持超过指定阈值后使用 Brotli、GZip、ZLib 或 Deflate 压缩消息负载；
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
			<server server.name="unnamed" />
		</servers>
	</option>
</configuration>
```

三个数字依次表示可靠性控制、发布者进站和订阅者出站端口。两段式配置表示 `Incoming,Outgoing`，此时 Server 挂载 Storage 后会随机绑定 Control 端口；仅当 Server 已挂载 `IMessageStorage` 时才启动 Control。端口优先级依次为启动参数、命名服务器自身的 `Port`、`Servers.Port` 默认值，仍未定义时才随机选择；`*` 明确要求随机端口。

独立应用可以直接启动交换服务：

```csharp
using var server = new ZeroQueueServer();
await server.StartAsync(["--incoming:32101", "--outgoing:32102"]);
```

### 客户端连接

在 `/Messaging/ConnectionSettings` 下定义 `ZeroMQ` 连接：

客户端名称与分组沿用[样例客户端](samples/client/Program.cs)的连接设置；这里转换成宿主选项格式：

```xml
<configuration>
	<option path="/Messaging">
		<connectionSettings default="ZeroMQ">
			<connectionSetting connectionSetting.name="ZeroMQ"
			                   driver="ZeroMQ"
			                   value="server=127.0.0.1;port=7969;group=Demo;client=Zongsoft.Messaging.ZeroMQ.Sample;" />
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

在已经部署 ZeroMQ 插件的宿主中，按提供者名和连接名取得公共队列。以下沿用上文的 `ZeroMQ` 连接；同一进程自发自收的演示需在该连接中配置 `filter=*`，并提前启动兼容 Broker。消费模块只引用 Core：

```csharp
using System.Text;
using Zongsoft.Services;
using Zongsoft.Messaging;

var provider = ApplicationContext.Current.Services
	.FindRequired<IMessageQueueProvider>("ZeroMQ");
var queue = provider.Queue("ZeroMQ");
var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var topic = $"docs/{Guid.NewGuid():N}";

var consumer = await queue.SubscribeAsync(topic, message =>
	received.TrySetResult(Encoding.UTF8.GetString(message.Data.Span)));

try
{
	var identifier = await queue.ProduceAsync(topic, "demo".AsMemory());
	if(identifier == null)
		throw new InvalidOperationException("No matching subscription was visible.");

	Console.WriteLine(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
}
finally
{
	await consumer.UnsubscribeAsync();
}
```

提供者按名复用队列，业务操作不要释放共享队列。示例仅取消自己创建的唯一主题订阅，并等待处理器完成后才退出；发送返回非空标识仍不等于业务处理完成。独立工具若直接构造 `ZeroQueue`，才由工具负责其完整生命周期。

主题订阅采用前缀匹配。一个 `ZeroQueue` 对每个逻辑主题只保留一个消费者；再次订阅同一个主题会返回已有消费者，不会替换处理器或选项。设置 `Group=Demo` 后，网络上的物理主题为 `Demo:topic/reliable`，处理器收到的 `Message.Topic` 仍为逻辑主题 `topic/reliable`。

同一订阅内的处理器按接收顺序串行执行。待处理队列达到容量后，该订阅会暂停从 Poller 接收，消费腾出空间后再恢复；背压只作用于相应订阅，不会阻塞其他 Socket。

### 压缩

通过 `MessageEnqueueOptions.Compression` 指定压缩算法和启用压缩的最小负载字节数。`MessageCompression.Value` 是以字节为单位的整数阈值，其文本格式为 `<algorithm>:<threshold>`；阈值为零时压缩所有非空负载，默认值表示不压缩：

```csharp
var options = new MessageEnqueueOptions()
{
	Compression = MessageCompression.Parse("Brotli:4096"),
};

await queue.ProduceAsync("documents/updated", payload, options);
```

对应的强类型构造方式为 `new MessageCompression("Brotli", 4096)`。

### 至少一次投递

订阅和发布都要显式选择 `LeastOnce`。只有 Broker 必须挂载 `IMessageStorage`；发布端不存储消息。处理器正常返回不代表确认，必须调用 `AcknowledgeAsync`。

下面摘自 [ZeroQueueReliabilityTests.LeastOnceCompletesAfterBrokerAcceptanceBeforeAcknowledge](test/ZeroQueueReliabilityTests.cs)。`ReliableServerScope`、`CreateQueue`、`AcknowledgingHandler` 等都是同一测试文件中的真实夹具；应在原测试项目中阅读或执行，不是可直接粘贴到业务插件的类型：

```csharp
await using var scope = await ReliableServerScope.StartAsync();
using var publisher = CreateQueue(scope.Port, "publisher", "publisher");
using var subscriber = CreateQueue(scope.Port, "subscriber", "subscriber");
var handler = new AcknowledgingHandler(1, false);
await subscriber.SubscribeAsync("topic/reliable", handler, ReliableSubscribeOptions());

var identifier = await publisher.ProduceAsync("topic/reliable", Encoding.UTF8.GetBytes("reliable"), ReliableEnqueueOptions()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

Assert.False(string.IsNullOrWhiteSpace(identifier));
Assert.Equal(identifier, message.Identifier);
Assert.Single(await GetPendingAsync(scope));

await message.AcknowledgeAsync();
Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
```

该夹具验证确认前 Pending 仍存在、确认后清除；它不是订单入库实现，也不证明业务副作用具有幂等性。测试存储的进程内行为不代表生产存储具备重启耐久性。

Broker 只在发送瞬间存在在线匹配订阅时接纳消息：没有订阅返回 `null` 且不写入 Storage；存在订阅则先持久化 Pending，再返回消息标识。之后按主题在在线订阅者间竞争投递，任一消费者确认即删除 Pending。未确认会沿用同一 `Message.Identifier` 重投，也可能改投另一个消费者，因此处理器必须保证业务幂等。

`ZeroQueueServer.Storages` 只能在 Server 停止时赋值。首次启动时 Server 调用 `Storages.Create(Name)`，并取得返回存储器的所有权；普通 Stop 保留它以供重启复用，替换工厂、启动失败或释放 Server 时优先调用 `IAsyncDisposable` 释放。Broker 未配置存储工厂时仍提供 `MostOnce` Broadcast，但不启动 Control，发现响应的 `Ports` 只包含 `Incoming,Outgoing`；此时 `LeastOnce` 操作会失败。

| 消息选项 | 支持情况 |
| --- | --- |
| `Compression` | `MostOnce` 与 `LeastOnce` 均支持 Brotli、GZip、ZLib、Deflate，只压缩 `Message.Data`。 |
| 标签与身份 | 两种模式都独立传递 `Identifier`、`Identity` 和 `Tags` 元数据。 |
| `Delay` | 不支持；正数请求由 Core 根据 `Features` 在进入驱动前拒绝。 |
| 过期时间 | `LeastOnce` 支持；零表示永不过期。 |
| 优先级 | 未实现。 |
| `MostOnce` | 支持；发送瞬间无匹配订阅返回 `null`，否则本地发送一次。 |
| `LeastOnce` | 支持 Broker 持久接纳、竞争消费、显式确认和同标识重投。 |
| `ExactlyOnce` | 不支持，并在创建传输状态前失败。 |
| 订阅失败策略 | 当前处理器调度器未实现。 |

### 请求与响应

`ZeroRequester` 与 `ZeroResponder` 通过逻辑 URL 主题交换请求，默认回复主题为 `<url>/reply`。真实用例见 [ZeroRequesterTests.RequesterReceivesImmediateResponses](test/ZeroRequesterTests.cs)：它启动独立请求与响应队列，注册同文件内的 `EchoHandler`，向 `rpc/echo` 发送消息并检查内容与请求标识一致。测试在 `finally` 中停止、释放响应器，请求令牌也单独释放。

[ZeroResponderTests](test/ZeroResponderTests.cs)进一步验证启动失败时回滚已建立的订阅。这里不提供不存在的 `PingHandler`；应用应通过公共通信契约和宿主装配使用响应器，独立测试才自行拥有实例生命周期。

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

消息存储是独立插件，不属于 ZeroMQ 驱动。本包不提供默认文件存储；需要 `LeastOnce` 时，应用注入 `IMessageStorageFactory`，工厂按 Broker 名查找同名连接并创建其独占存储器。Storage 支持按精确主题读取和清理；选择实现时应确认它能在 `SetAsync` 返回前持有消息快照，并满足所需的进程重启耐久性。

完整的发现、Broadcast 和 Control 帧定义参见 [ZeroMQ 1.0 协议](PROTOCOL.zh-Hans.md)。

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

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

部署清单按 `Zongsoft.Messaging.ZeroMQ-$(site).plugin` 选择附加插件；`site=daemon` 包含 Broker 启动贡献，必须明确区分纯客户端与 Broker 宿主。启动前配置端点及存储工厂。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Messaging.ZeroMQ.Daemon` | [Zongsoft.Messaging.ZeroMQ-daemon.plugin](src/Zongsoft.Messaging.ZeroMQ-daemon.plugin) |
| `Zongsoft.Messaging.ZeroMQ` | [Zongsoft.Messaging.ZeroMQ.plugin](src/Zongsoft.Messaging.ZeroMQ.plugin) |
| `Zongsoft.Messaging.ZeroMQ.Storage` | [Zongsoft.Messaging.ZeroMQ.Storage.plugin](src/Zongsoft.Messaging.ZeroMQ.Storage.plugin) |
| 文件复制及依赖 | [Zongsoft.Messaging.ZeroMQ.deploy](src/Zongsoft.Messaging.ZeroMQ.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft messaging zeromq]
nuget:Zongsoft.Messaging.ZeroMQ
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Messaging.ZeroMQ.option`、`Zongsoft.Messaging.ZeroMQ.plugin`、`Zongsoft.Messaging.ZeroMQ-$(site).plugin`、`Zongsoft.Messaging.ZeroMQ.Storage.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

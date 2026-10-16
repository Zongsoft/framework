# Zongsoft.Messaging.Mqtt 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Mqtt)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.Mqtt)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 客户端连接设置（MqttConnectionSettings）

连接设置（[`MqttConnectionSettings`](src/Configuration/MqttConnectionSettings.cs)）通过 `GetOptions()` 生成 MQTTnet 的 `MqttClientOptions`，由适配器处理客户端标识、认证和通道选项。`Server` 使用 `host:port`（如 `localhost:1883`）或连接 URI；端口写在 `Server` 中，当前描述器没有独立的 `Port` 主参数。

主参数由 `Driver.Descriptors` 定义，未知文本项进入扩展属性集（`Properties`）。`Topic`、`Server`、`ReconnectInterval` 等业务参数仍是已定义主参数，即使标记为 `Ignored` 也不应写入 `Properties`。完整 `Value` 包含主参数与扩展项，通用读写规则见 [Core 设置契约](../../Zongsoft.Core/docs/settings.zh-Hans.md)。

## 范例

`samples` 目录包含两个基于 .NET 10 的交互式应用：

- [Broker 范例](samples/server/Program.cs)使用 `MqttQueueServer` 启动 MQTT Broker。
- [客户端范例](samples/client/Program.cs)通过 `MqttQueue` 连接 Broker，可以订阅、取消订阅、发布和接收消息。

客户端默认连接 `127.0.0.1:1883`。每个客户端进程都会生成唯一的 ClientId，因此可以同时运行多个实例进行端到端验证。

> Broker 范例没有配置身份认证和 TLS，仅应在可信的开发或测试环境中使用。

### 环境准备与构建

安装 .NET 10 SDK。在仓库根目录中先构建核心库，再构建 MQTT 解决方案：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet build messaging/mqtt/Zongsoft.Messaging.Mqtt.slnx
```

### 1. 启动 Broker

打开第一个终端并运行：

```shell
dotnet run --project messaging/mqtt/samples/server/Zongsoft.Messaging.Mqtt.Samples.Server.csproj
```

Broker 范例会自动监听 `tcp://127.0.0.1:1883`。在交互式命令行中输入 `info`，可以确认运行状态并查看当前客户端通道和会话。
该范例还为 Broker 设置了一个 `IHandler<Message>`，因此客户端发布的每条消息都会显示在 Broker 终端中。

| 命令 | 说明 |
| --- | --- |
| `info [--topic:<topic>]` | 显示 Broker 状态、客户端通道、会话和所有保留消息；可选显示指定保留消息及其内容。 |
| `stop` | 停止 Broker 并释放监听端口。 |
| `start` | 按配置的 URL 重新启动 Broker。 |

### 2. 启动订阅客户端

打开第二个终端并启动客户端范例：

```shell
dotnet run --project messaging/mqtt/samples/client/Zongsoft.Messaging.Mqtt.Samples.Client.csproj
```

订阅测试主题：

```text
subscribe samples/demo
```

可以使用 `sub` 和 `unsub` 作为 `subscribe` 和 `unsubscribe` 的别名。范例也支持 MQTT 通配符过滤器，例如：

```text
subscribe samples/#
```

### 3. 发布并验证消息

打开第三个终端，使用相同的 `dotnet run` 命令启动另一个客户端范例，然后发布消息：

```text
produce --topic:samples/demo "来自 MQTT 范例的消息"
```

可以使用 `send` 作为 `produce` 的别名。发布客户端会输出 MQTT 报文标识和耗时，订阅客户端应显示类似以下内容：

```text
[Received]#1 Topic:samples/demo
[1]来自 MQTT 范例的消息
```

回到 Broker 终端输入 `info`，此时应显示两个客户端通道、关联会话以及所有保留消息的摘要。通道信息包括远程端点、协议版本、连接时间、报文数和字节数；会话信息包括生命周期时间、过期间隔和待发消息数。

如果 MQTT 应用或测试工具发布了保留消息，可以按主题查询：

```text
info --topic:samples/demo
```

该命令会显示保留消息的主题、载荷大小和 UTF-8 内容；如果该主题没有保留消息，则显示 `N/A`。

### 服务器状态 API

应用程序可以通过 `MqttQueueServer` API 查询相同的信息：

```csharp
var channels = server.Channels;
var sessions = server.Sessions;
var retainedMessages = await server.GetRetainedMessagesAsync();
var retained = await server.GetRetainedMessageAsync("samples/demo");

foreach(var channel in channels)
	Console.WriteLine($"{channel.Identifier}: {channel.Address}");

foreach(var session in sessions)
	Console.WriteLine($"{session.Identifier}: {session.PendingApplicationMessagesCount}");

foreach(var message in retainedMessages)
	Console.WriteLine($"{message.Topic}: {message.Data.Length} bytes");

if(!retained.IsEmpty)
	Console.WriteLine(Encoding.UTF8.GetString(retained.Data));
```

`ChannelCollection` 和 `SessionCollection` 分别以 ClientId、SessionId 作为集合键。Broker 会在客户端连接、断开以及会话创建、删除时自动同步这两个长生命周期集合，调用方无需刷新集合。Broker 未启动或没有保留消息时，`GetRetainedMessagesAsync()` 返回空数组；通过 `GetRetainedMessageAsync()` 查询空主题、已停止的服务器或没有保留消息的主题时，返回 `Message.Empty`。

`MqttQueueServer` 继承自 `ListenerBase<Message>`，可以通过设置 `Handler` 属性处理所有进入 Broker 的消息：

[真实服务端样例](samples/server/Program.cs)使用同一源码文件中定义的 `Handler.Instance`，向终端输出收到的消息：

```csharp
using var server = new MqttQueueServer() { Url = "tcp://127.0.0.1:1883", Handler = Handler.Instance };
await server.StartAsync([]);
```

接收到的 `Message.Identity` 为 MQTT ClientId，`Message.Topic` 为发布主题。未设置处理器时，服务器仍会作为普通 MQTT Broker 正常工作。

### 批量及并发验证

使用 `round` 选项重复发布相同的消息：

```text
produce --topic:samples/demo --round:1000 "批量测试消息"
```

订阅客户端会显示每条消息及其递增序号，发布客户端则输出总耗时，可用于快速检查消息吞吐能力。

### 自动重连验证

保持两个客户端运行，在 Broker 终端中依次输入：

```text
stop
start
```

等待客户端的重连间隔，默认是两秒，然后再次发布消息。`MqttQueue` 会自动重连并恢复现有订阅，因此订阅客户端无需重新执行 `subscribe` 即可收到消息。

### 客户端命令

| 命令 | 说明 |
| --- | --- |
| `info` | 显示连接设置和当前订阅。 |
| `subscribe <topic> [...]` | 订阅一个或多个主题或通配符过滤器，别名为 `sub`。 |
| `unsubscribe <topic> [...]` | 取消一个或多个订阅，别名为 `unsub`。 |
| `produce --topic:<topic> [--round:<count>] <message> [...]` | 发布一条或多条消息，别名为 `send`。 |
| `reset` | 重置接收消息的显示序号。 |
| `close` | 释放 MQTT 队列；如需继续测试，请重新启动客户端范例。 |

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册连接设置驱动，服务发现注册队列提供程序。解析 IMessageQueueProvider 前配置具名连接。主插件连接外部 Broker，加载它不会启动嵌入式 Broker。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Messaging.Mqtt` | [Zongsoft.Messaging.Mqtt.plugin](src/Zongsoft.Messaging.Mqtt.plugin) |
| 文件复制及依赖 | [Zongsoft.Messaging.Mqtt.deploy](src/Zongsoft.Messaging.Mqtt.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft messaging mqtt]
nuget:Zongsoft.Messaging.Mqtt
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Messaging.Mqtt.plugin`、`Zongsoft.Messaging.Mqtt.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

## 可选嵌入式 Broker

[daemon 插件](src/Zongsoft.Messaging.Mqtt-daemon.plugin) 将一个 `MqttQueueServer` 挂载到 `/Workbench/Messaging/Mqtt/QueueServer`，并加入 `/Workbench/Startup`。[同名配置文件](src/Zongsoft.Messaging.Mqtt-daemon.option) 同时提供运行设置和暴露声明：

```xml
<?zongsoft.tools.binding name="mqtt"
	path="Messaging/Mqtt/Server"
	filter="enabled:true"
	template="${option.url}"
	protocols="tcp"?>

<options>
	<option path="/Messaging/Mqtt">
		<server enabled="true" url="tcp://0.0.0.0:1883" />
	</option>
</options>
```

`protocols="tcp"` 是原生 MQTT/TCP 的常量模板。插件通过现有 option 解析器读取同一组 `Enabled` 与 `Url`；`enabled="false"` 同时阻止启动并排除绑定。应在启动前配置，运行期间不能更换 URL；配置重载不会自动重启服务器。

包内包含 daemon 的两份附属物。部署清单选择 `Zongsoft.Messaging.Mqtt-${site}.plugin` 及其同名 `.option`，`site=daemon` 对应此组合；部署这两份文件才装配嵌入式 Broker。仅加载主插件提供客户端接入，不创建监听。该装配复用宿主已有的插件运行时，无需修改宿主代码或另写服务总清单。

`MqttQueueServer.Url` 默认为 `tcp://0.0.0.0:1883/`，接受 IPv4、带方括号的 IPv6 地址或表示 IPv4 通配的 `*`，必须显式指定 1 至 65535 的端口。读取属性返回带根斜杠的规范化地址。只监听声明的地址族，`[::]` 不隐含 IPv4；拒绝 DNS 名称、非根路径、凭据、查询、片段及 IPv4 广播地址。此配置不提供 TLS 或 WebSocket 监听。

API 复用 `WorkerBase.Enabled` 和启停流程。启动取消或失败会释放 Broker 实例，并向启动调用者传播异常；并发启动／停止等待共享的生命周期操作，停止失败允许重试清理。MQTTnet 的启停方法不接受取消令牌，因此启动等待 SDK 调用结束后检查取消，停止一旦开始便完成清理。

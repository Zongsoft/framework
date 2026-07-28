# Zongsoft.Messaging.Mqtt 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Mqtt)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.Mqtt)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

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

Broker 会自动监听 `1883` 端口。在交互式命令行中输入 `info`，可以确认运行状态并查看当前客户端通道和会话。
该范例还为 Broker 设置了一个 `IHandler<Message>`，因此客户端发布的每条消息都会显示在 Broker 终端中。

| 命令 | 说明 |
| --- | --- |
| `info [--topic:<topic>]` | 显示 Broker 状态、客户端通道、会话和所有保留消息；可选显示指定保留消息及其内容。 |
| `stop` | 停止 Broker 并释放监听端口。 |
| `start` | 在 `1883` 端口重新启动 Broker。 |

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

```csharp
server.Handler = new MyMessageHandler();
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

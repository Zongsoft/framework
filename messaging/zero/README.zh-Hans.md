# Zongsoft.Messaging.ZeroMQ 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.ZeroMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 范例

`samples` 目录包含两个基于 .NET 10 的交互式应用：

- [服务器范例](samples/server/Program.cs)使用 `ZeroQueueServer` 启动 ZeroMQ 消息交换服务。
- [客户端范例](samples/client/Program.cs)通过 `ZeroQueue` 连接服务器，可以订阅、取消订阅、发布和接收消息。

服务器通过 `7969` 端口响应交换通道发现请求，并使用 `32101` 和 `32102` 作为范例的数据通道端口。客户端默认连接 `127.0.0.1:7969`，并使用 `Demo` 消息分组。

> 服务器范例会在所有网络接口上监听，并且没有身份认证或加密，仅应在可信的开发或测试环境中使用。

### 环境准备与构建

安装 .NET 10 SDK。在仓库根目录中先构建核心库，再构建 ZeroMQ 解决方案：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet build messaging/zero/Zongsoft.Messaging.ZeroMQ.slnx
```

### 1. 启动服务器

打开第一个终端并运行：

```shell
dotnet run --project messaging/zero/samples/server/Zongsoft.Messaging.ZeroMQ.Samples.Server.csproj
```

消息交换服务会自动启动，管理端口为 `7969`，传入和传出数据端口分别为 `32101` 和 `32102`。

| 命令 | 说明 |
| --- | --- |
| `info` | 显示服务器状态和管理端口。 |
| `stop` | 停止消息交换服务并释放三个端口。 |
| `start --incoming:32101 --outgoing:32102` | 使用范例的数据端口重新启动消息交换服务。 |

### 2. 启动订阅客户端

打开第二个终端并启动客户端范例：

```shell
dotnet run --project messaging/zero/samples/client/Zongsoft.Messaging.ZeroMQ.Samples.Client.csproj
```

订阅测试主题：

```text
subscribe samples/demo
```

客户端会自动在网络主题前添加已配置的 `Demo` 分组。所有客户端范例使用相同分组，因此可以互相通信。

### 3. 发布并验证消息

打开第三个终端，使用相同的 `dotnet run` 命令启动另一个客户端范例。每个队列都会生成唯一的实例标识，因此两个客户端可以同时保持连接。发布消息：

```text
produce --topic:samples/demo "来自 ZeroMQ 范例的消息"
```

可以使用 `send` 作为 `produce` 的别名。发布客户端会输出主题和耗时，订阅客户端应显示类似以下内容：

```text
[Received]#1 Topic:Demo:samples/demo
[1]来自 ZeroMQ 范例的消息
```

### 批量验证

使用 `round` 选项重复发布相同的消息：

```text
produce --topic:samples/demo --round:1000 "批量测试消息"
```

订阅客户端会显示每条消息及其递增序号，发布客户端则输出总耗时，可用于快速检查消息吞吐能力。

### 重启验证

保持两个客户端运行，在服务器终端中依次输入：

```text
stop
start --incoming:32101 --outgoing:32102
```

NetMQ 套接字会自动重新连接恢复后的端点。再次发布消息，确认现有订阅客户端无需重新订阅即可收到消息。

### 客户端命令

| 命令 | 说明 |
| --- | --- |
| `info` | 显示队列实例标识、连接设置和当前订阅。 |
| `subscribe <topic> [...]` | 订阅一个或多个主题，别名为 `sub`。 |
| `unsubscribe <topic> [...]` | 取消一个或多个订阅，别名为 `unsub`。 |
| `produce --topic:<topic> [--round:<count>] <message> [...]` | 发布一条或多条消息，别名为 `send`。 |
| `reset` | 重置接收消息的显示序号。 |
| `close` | 释放 ZeroMQ 队列；如需继续测试，请重新启动客户端范例。 |

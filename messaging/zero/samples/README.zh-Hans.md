# Zongsoft.Messaging.ZeroMQ 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [server](server) | 使用独立的消息流入和流出端点运行 ZeroMQ 队列服务端。 |
| [client](client) | 通过 `ZeroQueue` 演示主题订阅、消息发布和消息接收处理。 |

两个项目均面向 .NET 10。服务端在 `7969` 端口提供端点发现，并绑定流入端口 `32101` 和流出端口 `32102`；客户端连接 `127.0.0.1:7969`，再从服务端取得消息端点。

## 运行

在仓库根目录启动服务端：

```shell
dotnet run --project messaging/zero/samples/server/Zongsoft.Messaging.ZeroMQ.Samples.Server.csproj
```

然后在另一个终端启动客户端：

```shell
dotnet run --project messaging/zero/samples/client/Zongsoft.Messaging.ZeroMQ.Samples.Client.csproj
```

## 服务端命令

服务端会以 `--incoming:32101 --outgoing:32102` 自动启动。使用以下命令查看或重启服务：

```text
info
stop
start --incoming:32101 --outgoing:32102
```

`info` 输出工作状态和发现端口。传给 `start` 的选项会转交给 `ZeroQueueServer`，因此可修改消息流入和流出端点；客户端会通过 `7969` 端口自动发现这些变化。

## 客户端订阅

传入一个或多个主题即可订阅，`sub` 是 `subscribe` 的别名：

```text
subscribe demo notifications
sub telemetry
```

使用 `unsubscribe` 或 `unsub` 取消订阅：

```text
unsubscribe notifications telemetry
```

每条接收到的消息都会输出序号、主题和 UTF-8 正文。

## 客户端发布

必须通过 `--topic` 选项指定目标主题，每个位置参数都会成为一条独立消息：

```text
produce --topic:demo hello
produce --topic:demo first second third
```

使用 `--round:<次数>` 重复发布全部消息，`send` 是 `produce` 的别名：

```text
produce --topic:demo --round:3 "Hello ZeroMQ"
send --topic:telemetry --round:2 value-1 value-2
```

在客户端使用 `info` 显示连接设置和当前订阅，使用 `reset` 清空已接收消息计数，使用 `close` 释放队列。

## 建议场景

1. 启动服务端，再启动客户端；
2. 在客户端依次执行：

```text
subscribe demo
produce --topic:demo --round:3 "Hello ZeroMQ"
info
unsubscribe demo
```

3. 确认收到三条消息，并且 `demo` 已从订阅列表中消失；
4. 在服务端依次执行 `stop`、`info` 和 `start --incoming:32101 --outgoing:32102`，观察其生命周期。

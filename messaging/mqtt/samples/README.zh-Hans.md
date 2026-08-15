# Zongsoft.Messaging.Mqtt 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [server](server) | 运行 MQTT 代理，并显示通道、会话、保留消息和接收到的应用消息。 |
| [client](client) | 连接 MQTT 代理，演示消息发布、订阅、确认和队列信息查看。 |

两个项目均面向 .NET 10。客户端默认连接 `127.0.0.1:1883`，与服务端的默认监听端点一致。

## 运行

在仓库根目录启动代理：

```shell
dotnet run --project messaging/mqtt/samples/server/Zongsoft.Messaging.Mqtt.Samples.Server.csproj
```

然后在另一个终端启动客户端：

```shell
dotnet run --project messaging/mqtt/samples/client/Zongsoft.Messaging.Mqtt.Samples.Client.csproj
```

## 服务端命令

服务端会自动启动，可在其终端执行：

```text
info
info --topic:demo
stop
start
```

`info` 显示工作状态、监听端口、已连接通道、会话和全部保留消息；指定 `--topic:<名称>` 时，仅显示该主题的保留消息。使用 `stop` 和 `start` 可在不重启进程的情况下演示代理生命周期。

## 客户端订阅

传入一个或多个主题即可订阅，短别名为 `sub`：

```text
subscribe demo notifications
sub telemetry
```

使用 `unsubscribe` 或 `unsub` 取消订阅：

```text
unsubscribe notifications telemetry
```

每条接收到的消息都会输出 MQTT 客户端标识、主题和 UTF-8 正文。

## 客户端发布

必须通过 `--topic` 选项指定目标主题，每个位置参数都会成为一条独立消息：

```text
produce --topic:demo hello
produce --topic:demo first second third
```

使用 `--round:<次数>` 重复发布全部消息，`send` 是 `produce` 的别名：

```text
produce --topic:demo --round:3 "Hello MQTT"
send --topic:telemetry --round:2 value-1 value-2
```

在客户端使用 `info` 显示队列及当前订阅，使用 `reset` 清空已接收消息计数，使用 `close` 释放客户端队列。

## 建议场景

1. 启动服务端，再启动客户端；
2. 在客户端依次执行：

```text
subscribe demo
produce --topic:demo --round:3 "Hello MQTT"
info
```

3. 在服务端执行 `info`，确认列表中存在该客户端的通道和会话；
4. 在服务端执行 `stop` 和 `start` 观察客户端连接生命周期，最后在客户端执行 `unsubscribe demo`。

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

1. 启动服务端和两个客户端进程。未修改的 [client/Program.cs](client/Program.cs) 使用 `Group=Demo` 和默认过滤规则，会排除同一队列实例发布的消息；按此配置，单个客户端无法演示自身发布消息的回环接收。
2. 在客户端 A 订阅，并等待命令完成：

```text
subscribe demo
```

3. 在客户端 B 发布：

```text
produce --topic:demo --round:3 "Hello ZeroMQ"
```

4. 在客户端 A 观察接收输出，再查看和取消订阅：

```text
info
unsubscribe demo
```

5. 匹配路由就绪且没有其他生产者时，预期收到三条消息；`MostOnce` 不提供确认或补发保证。发布标识为空表示发布当时没有匹配路由。解释输出前先阅读[投递与过滤规则](../README.zh-Hans.md)。
6. 在服务端依次执行 `stop`、`info` 和 `start --incoming:32101 --outgoing:32102`，观察其生命周期。

## 安全与清理

🚨 [示例服务端](server/Program.cs) 未挂载消息存储，也未建立身份认证或加密。监听器绑定网络接口，请通过本地环境、防火墙隔离端口，不要对公网开放。这个范例演示广播投递，不是持久化恢复。

客户端取消订阅后执行 `close`、`exit`（确认退出提示）；服务端执行 `stop`、`exit`。该范例未创建需要擦除的持久消息存储。插件部署和公共队列提供者用法见[类库说明](../README.zh-Hans.md)；此处直接构造队列仅属于独立诊断工具的用法。

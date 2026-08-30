# Zongsoft.Net 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [server](server) | 运行带长度包头的 TCP 服务端，显示收到的 UTF-8 消息并广播确认响应。 |
| [client](client) | 连接 TCP 服务端，发送或接收带长度包头的 UTF-8 消息。 |

两个项目均面向 .NET 10，并使用带长度前缀的 `TcpServer.Headed` 和 `TcpClient.Headed` API。默认端点为 `127.0.0.1:7969`。

## 运行

在仓库根目录启动服务端：

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj
```

然后在另一个终端启动客户端：

```shell
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj
```

服务端可接受可选的 IP 地址和端口，客户端也接受相同的两个位置参数：

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -- 0.0.0.0 9000
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -- 127.0.0.1 9000
```

## 命令

客户端支持 `connect`、`disconnect`、`info` 和 `send <消息>`；发送消息时如未连接会自动建立连接。

服务端支持 `start [地址] [端口]`、`stop`、`info` 和 `broadcast <消息>`；每条客户端消息都会显示出来，并以 `ACK:` 前缀向所有已连接客户端广播确认响应。

## 建议场景

1. 启动服务端和客户端；
2. 在客户端执行 `connect` 和 `send Hello Zongsoft.Net`；
3. 确认服务端显示消息，客户端收到 `ACK: Hello Zongsoft.Net`；
4. 在服务端执行 `broadcast Maintenance starts soon`，确认客户端收到广播。

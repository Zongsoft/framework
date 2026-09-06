# Zongsoft.Net 网络插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Net)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Net)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**N**et](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Net) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的高性能网络通讯类库，提供了有关 _**S**ocket_ 网络开发的各项功能。

它基于 `System.IO.Pipelines` 与 `Pipelines.Sockets.Unofficial` 提供异步 TCP 客户端、服务器、通道、连接计量、广播和封包。适用于 HTTP、gRPC 或消息代理不合适的应用专属二进制协议。

## 数据包与字节流

TCP 是字节流，一次发送并不保证对应一次接收。`IPacketizer<T>` 定义消息边界：`Headed` 使用 4 字节大端长度前缀并产生 `ReadOnlySequence<byte>`；`Headless` 把当前可用字节作为 `IMemoryOwner<byte>` 数据包；自定义 `TcpClient<T>`/`TcpServer<T>` 可接收应用封包器。

多数消息协议应使用 `Headed`；只有其他协议层或连接生命周期能明确分隔消息时才使用 `Headless`。

## 安装与客户端示例

```shell
dotnet add package Zongsoft.Net
```

```csharp
using System.Buffers;
using System.Net;
using System.Text;
using Zongsoft.Net;

var client = TcpClient.Headed;
client.Address = new IPEndPoint(IPAddress.Loopback, 7969);
await client.ConnectAsync();
await client.SendAsync(
	new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("hello")),
	cancellationToken);
await client.DisconnectAsync(cancellationToken);
```

设置 `IHandler<T>` 接收数据包。客户端发送时可延迟重连并报告累计字节数；服务器为每个连接管理通道并支持广播。

> 💡 可运行[范例](samples/README.zh-Hans.md)在 `127.0.0.1:7969` 演示处理器、启动/停止、发送与广播。

## 生命周期与安全

优雅停机时应释放或断开客户端并停止服务器。请传递取消信号；不同连接的处理器可能并发运行。

🚨 封包不等于安全。请增加身份验证、完整性保护、加密、包大小限制、超时和准入限制，切勿把范例明文监听暴露到不可信网络。

自定义封包器只能在完整帧到达后消费字节。超出回调生命周期保存序列或池化内存可能破坏数据或泄漏缓冲区。

## 延伸阅读

- [可运行范例](samples/README.zh-Hans.md)
- [System.IO.Pipelines](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines)
- [TCP 规范](https://www.rfc-editor.org/rfc/rfc9293)
- [实现协作指南](SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

清单贡献 FTP 文件系统提供程序；TCP API 示例仍是由应用显式创建的网络对象，加载插件不会自动启动 TCP 监听器。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Net` | [Zongsoft.Net.plugin](src/Zongsoft.Net.plugin) |
| 文件复制及依赖 | [Zongsoft.Net.deploy](src/Zongsoft.Net.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft net]
nuget:Zongsoft.Net
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Net.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

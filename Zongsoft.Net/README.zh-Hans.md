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

它提供异步 TCP 客户端、服务器、通道、连接统计、广播和分帧，支持 .NET 8、9、10，仅要求 `Microsoft.NETCore.App` 运行时。适用于设备长连接、心跳、中小消息交互和设备合批等应用专属协议。

## 数据包与字节流

TCP 是字节流，一次发送不对应一次接收。`Headed` 使用四字节 **无符号大端正文长度**；`Headless` 返回当前可用的有界字节块，不定义业务消息边界。两者均输出借用的 `ReadOnlySequence<byte>`，不复制完整正文。默认正文上限为 64 KiB，空正文仅在 Headed 中形成完整帧。

自定义协议派生 `PacketizerBase<T>`，遵循以下扩展契约。分帧器必须能跨连接并发使用，不能把连接状态存进共享实例。

| 操作 | 契约 |
| --- | --- |
| `GetLength` | 返回包含包头的精确线协议字节数，不改变报文 |
| `Pack` | 向给定写入器编码一帧，写入量等于声明长度，不主动刷新写入器 |
| `Unpack` | 数据不足时返回 `false` 且输入不变；成功必须消费至少一个字节，非法数据抛出异常 |
| `Release` | 每份成功解码的报文在处理完成或失败后释放一次；默认无操作，拥有资源的分帧器应重写 |

连续到达的完整帧会按序交给处理器，批尾不完整帧等待后续字节；合法多帧批次可以大于单帧上限。若协议把多个业务记录封装为一个外层帧，则整体适用单帧上限，内部记录及解压大小由应用限制。对端结束输入时，完整帧及其响应可以处理完毕；残留不完整帧会导致协议错误。

处理器接收 `TcpReceiveContext<T>`，通过 `context.Channel.SendAsync(context.Package, cancellation)` 回复发起连接。不同连接并发调用处理器，同一连接逐帧等待处理完成。借用数据仅在本次异步处理完成前有效；需要后台处理或长期保存时自行复制。不要把未等待的任务留在回调之外继续访问报文。

对于默认二进制报文，导入 `Zongsoft.Common` 后，在处理器结束前调用 `context.Package.Lease()`，可将序列复制到池化的 `IMemoryOwner<byte>` 中，独立于原借用报文存活。将租约移交后台消费者后，由消费者在使用结束时释放；移交失败则由当前处理器释放。`Memory<T>.Owned()` 只是包装原内存，不能延长借用缓冲的寿命。

## 创建与收发

```shell
dotnet add package Zongsoft.Net
```

`TcpClient.Headed/Headless`、`TcpServer.Headed/Headless` 每次返回独立实例。使用 `CreateHeaded(options)`、`CreateHeadless(options)` 指定选项，或将派生自 `PacketizerBase<T>` 的自定义打包器传入泛型构造函数。选项在构造时复制，之后修改原选项不影响已有实例。

支持 IPv4 和 IPv6 端点。服务端端口设为零时，启动后的 `Address` 返回实际分配的端点；客户端使用该端点连接。

```csharp
using System;
using System.Net;
using System.Threading;

using Zongsoft.Net;
using Zongsoft.Common;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await using var client = TcpClient.CreateHeaded(new() { MaximumPendingSends = 32 });
client.Address = new IPEndPoint(IPAddress.Loopback, 7969);
await client.ConnectAsync(cancellation: cancellation.Token);

using(var message = "hello".Encode())
	await client.SendAsync(message.Memory, cancellation.Token);

await client.DisconnectAsync(cancellation.Token);
```

此片段演示本地提交和退出；需要确认响应时，应设置 `IHandler<TcpReceiveContext<ReadOnlySequence<byte>>>` 并等待业务确认后再断开。完整请求响应、广播、心跳和合批见[可运行范例](samples/README.zh-Hans.md)。内存发送扩展同样经过分帧器，不能绕过协议编码。

`Buffer.Encode` 默认使用 UTF-8 并返回池化内存。必须等待发送或整个广播完成后再释放所有者，取消和失败路径同样需要释放。发送不转移调用者的报文所有权，发送完成前不能修改或释放底层数据。接收文本可使用直接支持跨段输入的 `Encoding.GetString(ReadOnlySequence<byte>)`。

同一连接并发发送的帧不会交错，但不同调用者的调用时刻不构成严格发送顺序；需要确定顺序时，逐次等待发送完成。未配置处理器时忽略收到的报文；注入处理器的生命周期由提供者管理。

并发首次发送共用建连尝试。首次建连者的取消令牌控制尝试，其他调用者仅取消自己的等待。更改目标地址会使旧尝试失效并关闭旧连接。断开后再次发送可重新建连，失败消息不会自动重放。

## 关闭、背压与超时

- `SendAsync` 完成表示报文已提交本地发送流程并通过背压等待，不表示远端收到或业务确认。写入后取消会终止连接，远端是否收到仍不确定。
- `CloseAsync` 关闭传输；`WaitAsync(cancellation)` 等待处理器、已接纳发送、缓冲清理和生命周期通知全部完成。等待不主动关闭连接，取消只结束当前等待，通道故障仍可由之后的等待者观察。处理器可以等待 `CloseAsync`，但不能等待包含自身的 `WaitAsync`、`DisposeAsync` 或所属服务的停止。
- `DisconnectAsync`、`StopAsync` 等待当前处理和发送排空；超时强制关闭传输并报错。拒绝响应取消的处理器继续持有借用内存，直到实际退出才归还、移除通道。停止失败保持 `Stopping`，完成收尾后可再次停止。
- `BroadcastAsync` 限制全局广播次数和每次并发发送数，返回 `TcpBroadcastResult`，分别统计 `Succeeded/Failed/Rejected`；调用者取消向上传播。连接集合是实时视图，不承诺包含并发新连接。

| 设置 | 默认值 | 含义 |
| --- | --- | --- |
| `MaximumPacketLength` / `MaximumFrameLength` | 65,536 / 65,540 字节 | 正文与完整线协议帧上限 |
| `MaximumBufferedReceiveBytes` | 128 KiB | 未完成帧保留上限，不限制合法多帧批次总量 |
| `SendPauseThreshold` / `SendResumeThreshold` | 64 KiB / 32 KiB | 发送缓冲暂停／恢复阈值 |
| `ReceivePauseThreshold` / `ReceiveResumeThreshold` | 64 KiB / 32 KiB | 接收缓冲暂停／恢复设置；实际暂停阈值会保证合法帧可接收 |
| `ReceiveFrameBudget` / `ReceiveByteBudget` | 64 帧 / 64 KiB | 连续同步处理预算；至少处理一帧，耗尽后让出执行 |
| `MaximumPendingSends` / `MaximumPendingSendBytes` | 64 次 / 256 KiB | 等待、编码及尚未完成 Socket 发送的接纳额度，超额立即抛出错误码为 `TcpError.SendRejected` 的 `TcpException` |
| `MaximumConnections` / `Backlog` | 10,000 / 1,024 | 服务端连接上限与受理积压 |
| `BroadcastConcurrency` / `MaximumConcurrentBroadcasts` | 32 / 1 | 每次广播的并发度与广播接纳上限 |
| `ConnectTimeout` / `ShutdownTimeout` | 10 秒 / 10 秒 | 建连与停止期限 |
| `SendTimeout` / `FrameTimeout` | 30 秒 / 30 秒 | 无发送进展与首个残留字节起的完整帧接收期限 |
| `IdleTimeout` / `MaintenanceInterval` | 禁用 / 1 秒 | 无实际接收活动的期限与共享扫描周期 |
| `NoDelay` / `ZeroLengthReads` | 开启 / 开启 | 小消息及时发送与空闲接收缓冲策略 |

缓冲阈值用于流控，不是进程总内存的硬上限。心跳间隔应小于空闲期限并预留抖动和监测周期；持续滴入字节不会重置 `FrameTimeout`。设备身份、业务确认、命令关联、重试幂等及心跳格式由应用协议定义。

字节统计来自实际收发完成量；帧发送数表示本地提交完成。关闭连接的统计累积到所属客户端或服务端，并发读取是近似快照。启用 `EnableDiagnostics` 后可通过通道 `Statistics`（`TcpChannelStatistics`）观察读取次数、预算耗尽、单次读取最大帧数和发送等待次数。

启用诊断后，客户端及服务端的 `Statistics` 返回 `TcpMonitorStatistics`，提供共享连接监测的累计快照：`ScanCount`、`MaximumScanDuration`、`OverrunCount` 和 `MaximumTimeoutDelay`。同一 `TimeProvider` 与维护周期共享统计；组内存在启用诊断的连接时才累计，全部连接退出后仍保留已有值。未启用诊断的实例返回零值，读取属性不触发新的扫描。超时检测延迟相对于记录的期限计算，不包含关闭及业务退出时间；收发进度另有最多一个实际采样间隔的观测误差。扫描耗时包含调度等待，超期时实际采样间隔可能大于配置周期；`OverrunCount` 非零表示需要检查容量或调度负载。

本库的 TCP 入口不提供认证、TLS、UDP、QUIC 或业务会话持久化。需要认证或加密时，由应用提供相应协议保护；范例默认使用回环地址。

## 通道生命周期通知

在 `server.Channels` 或 `client` 上订阅以下事件，统一观察其拥有的通道。事件参数 `TcpChannelEventArgs<T>` 提供 `Channel`、`CloseReason` 和 `Error` 的不可变快照；正常关闭的 `Error` 为 `null`。

| 事件 | 触发时机 |
| --- | --- |
| `Closing` | 开始关闭传输，不可取消；当前处理器可能仍在运行 |
| `Error` | 开始关闭或最终清理时报告首个通道故障，每条通道最多一次 |
| `Closed` | 处理器、发送和缓冲清理完成，通道已从所有者移除，统计已累计 |

关闭开始时已经存在的故障按 `Closing`、`Error`、`Closed` 顺序通知；后续处理器或清理故障在最终 `Closed` 前报告，不修改此前交付的快照。关闭原因保留首个原因，异常保留首个非空故障，因此主动关闭后处理器失败时，最终快照可以同时包含 `Local` 和该异常。容量拒绝及尚未形成通道的建连失败由相应调用返回，不触发通道 `Error`。

事件同步调用；每个订阅者的异常单独记录并隔离，不阻止后续订阅者或资源清理，也不递归触发 `Error`。回调应及时返回，不得阻塞等待包含自身的停止、释放或 `WaitAsync`。应在启动或建连前订阅；完成后才开始等待时，使用仍能观察原始完成结果的 `WaitAsync`。

通道自身的 `Closing`、`Closed` 表示传输关闭阶段；上述客户端／管理器的 `Closed` 表示通道已完整清理。需要确认资源全部归还时，等待通道 `WaitAsync` 或使用所有者的 `Closed`。

## 异常处理

库自身识别的通讯错误统一使用 `TcpException`，通过 `ErrorCode` 判断原因。异常继承 `IOException`，默认消息取自本地化资源；调用方不要通过消息文本判断错误。

| `TcpError` | 含义 |
| --- | --- |
| `SendRejected` | 发送或广播容量不足，本次操作尚未被接纳 |
| `InvalidFrame` | 报文长度、实际编码长度或解码消费不符合分帧契约 |
| `IncompleteFrame` | 对端结束输入时仍留有不完整帧 |
| `ConnectionClosed` | 连接关闭或服务停止，当前操作无法继续 |

广播只把 `SendRejected` 计入 `Rejected`，其他通道发送异常计入 `Failed`；调用者取消仍向上传播。异常错误码描述本次操作失败，连接最终状态通过所有者的 `Closed` 事件参数观察，等待完整清理使用通道 `WaitAsync`。容量拒绝及尚未写入的编码错误不会主动关闭连接。

取消、超时、底层 Socket 和业务处理器异常不会一律转换为 `TcpException`；服务停止时汇总的受理故障通过 `InnerException` 保留原始异常。自定义分帧器的 `InvalidDataException` 归类为协议错误。可用 `catch(TcpException exception) when(exception.ErrorCode == TcpError.SendRejected)` 单独处理容量拒绝。

## 延伸阅读

- [可运行范例](samples/README.zh-Hans.md)
- [性能测试与负载程序](benchmark/README.zh-Hans.md)
- [System.IO.Pipelines](https://learn.microsoft.com/zh-cn/dotnet/standard/io/pipelines)
- [TCP 规范](https://www.rfc-editor.org/rfc/rfc9293)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

清单贡献 Web 文件系统提供程序；TCP API 示例仍是由应用显式创建的网络对象，加载插件不会自动启动 TCP 监听器。

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

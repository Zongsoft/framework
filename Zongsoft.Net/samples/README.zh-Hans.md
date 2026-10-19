# Zongsoft.Net 范例

[English](README.md) | [简体中文](README.zh-Hans.md)

两个范例项目面向 .NET 10，Net 库支持 .NET 8、9、10。安装 .NET 10 SDK，运行前先以 Debug 构建本地 Core。默认端点为 `127.0.0.1:7969`，无需数据库、Broker 或真实设备。

| 项目 | 用途 |
| --- | --- |
| [server](server/Program.cs) | 独立 TCP 服务，逐帧向当前通道回复；广播是独立命令 |
| [client](client/Program.cs) | 交互发送、自动请求响应、心跳和有界设备合批 |
| [共享协议](shared/SampleProtocol.cs) | 可选 LF 文本分帧器，演示精确长度、失败不消费和成功推进 |

## 交互运行

先在仓库根目录执行一次 `dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -p:GeneratePackageOnBuild=false`，然后在两个终端分别执行：

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -f net10.0 -- 127.0.0.1 7969
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -f net10.0 -- 127.0.0.1 7969
```

客户端命令：`connect`、`disconnect`、`info`、`send <消息>`。发送前尚未建连时自动建连。服务端命令：`start`、`stop`、`info`、`broadcast <消息>`。普通请求收到 `ACK: <消息>`，只回复发起连接；`PING` 返回 `PONG`。广播输出成功、失败和容量拒绝数。

可启动两个客户端验证响应路由：客户端一发送消息时，客户端二不收到该 ACK；服务端执行广播时，两端都收到广播。

## 自动演示与冒烟

以下命令同样分两个进程执行。服务端 `--count N` 在处理 N 条请求后排空停止，最长等待 60 秒；客户端自动模式验证响应后退出。服务端打印 `READY` 和 `STOPPED`，客户端成功后打印 `DISCONNECTED` 或 `BATCH completed=17`。端口可以使用零，由 `READY` 给出实际端口。

普通请求响应：

```shell
dotnet run --project Zongsoft.Net/samples/server/Zongsoft.Net.Samples.Server.csproj -f net10.0 -- 127.0.0.1 7969 --count 1
dotnet run --project Zongsoft.Net/samples/client/Zongsoft.Net.Samples.Client.csproj -f net10.0 -- 127.0.0.1 7969 --smoke
```

自定义分帧：共享的 `LinePacketizer` 派生自 Net 的 `PacketizerBase<ReadOnlySequence<byte>>`，实现 `GetLength`、`Pack`、`Unpack`，借用序列沿用默认无操作的 `Release`。两端都加 `--custom`；服务端仍用 `--count 1`，客户端仍用 `--smoke`。文本正文不能包含 LF，最大 64 KiB，尾部的 LF 是线协议的一部分。Headed 与此协议不能混用。

长连接心跳：服务端用 `--count 3`，客户端用 `--heartbeat`。演示每秒发送一次 `PING` 并等待 `PONG`，共三次。生产环境可按设备需要使用 60—300 秒间隔和随机抖动，同时配置更宽裕的空闲期限；收到字节不等于业务健康确认。

## 设备合批与半包

服务端用 `--count 17`，客户端用 `--batch`。该模式仅使用 Headed：模拟器用原始 Socket 控制设备端写入，积累最多 17 个小帧，主动等待 20 毫秒后发送前 16 个完整帧及下一帧的前两个包头字节；验证 16 个有序响应后，再发送剩余字节并验证最后响应。

库收到多帧后逐帧调用处理器；批尾半包留到后续读取，达到处理预算后仍继续处理已有完整帧。模拟器只在设备侧使用有界积累，不把整个批次作为一条大消息。主动等待会增加消息延迟，TCP 可能合并或拆分任意一次写入；本演示不承诺网络保留批次边界。

## 编码与缓冲复用

文本发送、回复与广播使用 Core 的 `Buffer.Encode` 扩展获得 UTF-8 池化租约；在 `using` 作用域内等待发送或广播完成，再归还租约。设备合批在 `Pack` 将内容复制到合批写入器后归还每条消息的租约。接收保持借用序列，直接按跨段输入解码为字符串；需要在回调结束后保存原始报文时，可使用 `context.Package.Lease()` 并明确移交和释放所有者，详见[类库契约](../README.zh-Hans.md)。

## 可选诊断输出

两端均可添加 `--diagnostics`，显式订阅 `TcpDiagnostics.Error`。共享 [SampleDiagnostics](shared/SampleDiagnostics.cs) 在回调中仅尝试写入容量为 64 的队列，由独立消费循环输出操作、异常类型和发生时捕获的 TraceId；队满丢弃并计数，退出时解除订阅并排空。此示例不输出报文、地址或异常正文。通道处理故障仍通过通道事件和 `WaitAsync` 观察；是否转换为日志由应用决定。

指标及连接跟踪可由宿主订阅 `Zongsoft.Net` 来源，沿用 Diagnostics 的 OTLP/Console 或 Diagnostics.Web 的 Prometheus 导出，见[遥测与日志策略](../README.zh-Hans.md#遥测与日志策略)。范例自身不创建导出器。

## 取消与清理

两个应用都使用独立实例、`await using` 和取消令牌。交互模式需要终端，自动模式可重定向输出；Ctrl+C 请求取消并在退出时清理。正常结束会等待发送排空，自动模式超时或响应错误以失败退出。ACK 只是应用示例文本，不是持久化确认。

`CloseAsync` 关闭传输，`WaitAsync` 等待处理与资源清理全部完成；处理器不能等待包含自身的完整退出。借用序列仅在当前异步处理完成前有效。参见[类库契约](../README.zh-Hans.md)。样例没有认证或加密，保持回环绑定；改成公网监听前应由应用补齐相应协议保护。

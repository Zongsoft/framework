---
name: zongsoft-net
description: 修改或审查 Zongsoft.Net 的 TcpClient、TcpServer、Channel、Packetizer、缓冲区与收发关闭竞态时使用；不用于消息代理专有线协议或业务 HTTP 客户端。
---

# Zongsoft.Net 实现协作

先阅读 [AGENTS.md](AGENTS.md)；公开配置、所有权和使用示例见 [README](README.zh-Hans.md)。

## 源码与职责

- [TcpClient](src/TcpClient.cs) 提供客户端入口、报文派发和通道通知；私有嵌套 [Connector](src/TcpClient.Connector.cs) 拥有地址代次、当前及退场中的连接、累计统计和断开／释放协调，其嵌套 Attempt 拥有单次建连的目标、取消源和完成信号。建连使用官方 Socket API。
- [TcpServer](src/TcpServer.cs) 管理服务状态、通道登记和累计统计；[受理流程](src/TcpServer.Accepting.cs) 将唯一 Accept 循环限定为启动方法的局部函数。私有嵌套 [Broadcaster](src/TcpServer.Broadcasting.cs) 拥有广播接纳、并发派发和停止排空状态。
- [TcpTransport](src/TcpTransport.cs) 是内部第三方传输唯一所有者，以 IDuplexPipe 为内部数据边界，不向公共 API 暴露 SocketConnection 或第三方选项；使用 Pipelines.Sockets.Unofficial 2.2.16，升级时验证收发、背压和释放语义。运行只依赖 Microsoft.NETCore.App。
- [TcpChannelBase](src/TcpChannelBase.cs) 协调接收结束、发送排空、关闭和最终完成。私有嵌套 [Receiver](src/TcpChannelBase.Receiving.cs) 独占输入借用缓冲、分帧进度、处理预算和接收超时；私有嵌套 [Sender](src/TcpChannelBase.Sending.cs) 独占输出编码、接纳额度、写入锁、排空和发送超时，其限长 FrameWriter 继续嵌套。具体通道仅向所属处理器派发并归还登记。
- 通道 `Statistics` 汇总收发组件的诊断计数，类型为 `TcpChannelStatistics`，由 `EnableDiagnostics` 开关控制。组件只暴露宿主所需的操作，不把锁、取消源或完成信号交给其他组件修改。
- 客户端、连接器及服务端以 Unregister 注销已经清理的通道，累计统计并归还登记／接入额度；通道管理器的 Remove 仍是集合操作。BroadcastAsync 返回专用 [TcpBroadcastResult](src/TcpBroadcastResult.cs)，不将广播计数泛化为服务端任意操作的结果。
- 客户端与 [TcpServerChannelManager](src/TcpServerChannelManager.cs) 提供 Closing／Closed／Error；[TcpChannelEventArgs](src/TcpChannelEventArgs.cs) 携带 Channel、CloseReason、Error 不可变快照。[TcpChannelNotifier](src/TcpChannelNotifier.cs) 是两个所有者共享的内部通知实现，逐个隔离并记录订阅者异常，不递归派发 Error。
- [TcpException](src/TcpException.cs) 与 [TcpError](src/TcpError.cs) 统一库自身的帧、容量和关闭状态错误。标准取消、超时、Socket 和处理器异常不一律包装；停止时汇总的受理故障保留 InnerException；广播只将 SendRejected 计为容量拒绝，其他错误计为失败。
- [PacketizerBase<T>](src/PacketizerBase.cs) 实现 Core 的 IPacketizer<T>，在 Net 层定义 GetLength 与 Release；客户端、服务端及通道统一接收此基类。Core 的两种 IPacketizer 接口只定义 Pack 与 Unpack。自定义协议派生该基类，资源型解码报文重写 Release。
- [Packetizer](src/Packetizer.cs) 的 Headed/Headless 都输出借用序列。Headed 是 UInt32 大端正文长度，空帧合法；Headless 是有界原始流片段。业务边界不得依赖 TCP 分段。
- [TcpConnectionMonitor](src/TcpConnectionMonitor.cs) 按时钟和周期共享扫描任务，代次串联防止并行扫描；最后连接退出后停止。连接不各自保有空闲计时器。每 256 条连接让出后更新单调时间；启用诊断时记录共享扫描耗时、超期次数及相对于记录期限的最大检测延迟，不能将其解释为某个服务实例独占的计数。
- 客户端和服务端的 Statistics 返回 [TcpMonitorStatistics](src/TcpMonitorStatistics.cs)，通道的同名属性返回 TcpChannelStatistics。前者是共享连接监测统计，后者是当前通道收发统计；属性只读取现有计数，不主动采集或扫描。FrameTimeout 同时用于关闭原因和选项，表示从首个残留字节开始的完整帧接收期限，持续滴入字节不延期。
- 泛型处理器参数为只读值类型 `TcpReceiveContext<T>`；回复使用当前通道，复制上下文不延长报文借用寿命。协议专有 topic、ack 或重投属于上层，不加入本库或 Core 通用契约。

## 配置与连接所有权

- 客户端与服务端创建入口每次返回独立实例；构造时复制并验证选项，运行中不读取调用者可变配置。端点地址族必须匹配 Socket；端口零返回实际绑定端点，启动参数不静默覆盖已配置地址。
- Socket 创建、连接、配置、传输包装与通道登记的每个失败分支都有唯一释放者；不得依赖 ConcurrentDictionary.GetOrAdd 的工厂只执行一次。并发首次连接共用 Attempt，首调用者控制尝试取消，其余调用者只取消自身等待。
- Connector 用地址代次阻止过期连接发布；断开、释放和地址变更使旧尝试失效。旧通道注销不能移除新通道；断开后的发送可以重新建连，不自动重放失败或结果不确定的消息。
- 服务端接入额度包含已接纳而尚在初始化的连接，失败与退出只归还一次。Accept 循环不等待单连接退场；先登记后启动接收，以稳定连接标识区分同一远端地址的多次连接。
- 收发 Pipe 使用共享内存池与线程池调度，useSynchronizationContext 为 false。默认开启 NoDelay 和 ZeroLengthReads，不启用 InlineReads、InlineWrites 或 PipeScheduler.Inline；不为每个连接预租大块接收缓冲或默认配置巨大 Socket 缓冲。

## 所有权与并发

- 构造不启动接收。连接登记后启动唯一接收循环；后台故障由所有者的 Error 通知记录，并保留给 `WaitAsync` 的当前及后续等待者。
- `CloseAsync` 结束传输；`WaitAsync(cancellation)` 等待处理器、发送、借用内存清理与生命周期通知，不主动关闭连接，取消只影响当前等待者。关闭不得提前完成仍被处理器使用的输入 Pipe。
- 所属对象通过通道的内部 `ShutdownAsync` 请求排空并等待完整退场，单连接故障由 `Error` 与公开 `WaitAsync` 保留；不能直接将通道故障任务并入所属对象的停止结果，导致结果随注销时机变化。真正的停止／排空超时及受理故障仍传播，业务处理器抛出的 `TimeoutException` 不等于排空超时。
- 所有者 Closing 在关闭开始时触发，Closed 在清理并移除登记后触发；通道继承的 Core Closed 仅表示传输关闭。每通道 Error 最多一次，已知故障在 Closing 后报告，迟到故障在最终 Closed 前报告；容量拒绝及尚未形成通道的建连失败仍由对应调用返回。
- 所有者事件在连接管理锁外同步调用；回调不能阻塞等待包含自身的完整停止／释放。关闭原因与首个非空异常分别保留，已交付的事件快照不可变，不以同步通知代替可在完成后继续等待的契约。
- `Unpack` 成功后，在处理器等待结束的 finally 中调用 Release；失败不得消费，成功必须推进。内存扩展发送仍经过分帧器。
- 保留必要短临界区：客户端地址与建连代次、连接发送接纳复合额度、服务广播接纳、共享维护任务登记。编码、回调和 I/O 不进入同步临界区；不同连接收发没有全局互斥。
- Interlocked 用于取得状态转换和唯一释放权；Volatile 只提供可见性与顺序，不能保证多项额度、关闭状态和等待登记的复合原子性。明确操作生效点、失败回滚及唯一归还者，覆盖丢失唤醒与过期完成；不使用 lock(this)、长时间自旋或同步 Wait／Result 等待 I/O 和处理器。
- 接收预算耗尽时 examined 留在 consumed 位置，剩余完整帧不依赖下次写入；不完整尾帧 examined 推进到末端；异常、取消和停止路径也不能回退既有 examined，避免 Pipe 推进异常掩盖原始故障。实际接收暂停阈值至少为残留上限加一，避免合法帧死锁。
- 发送字节额度覆盖预留与已编码但尚未完成 Socket 发送的字节。成功 Flush 不能提前归还这些字节；拒绝或取消排队时归还预留。编码后失败终止连接，关闭后归还废弃额度。
- 停止先禁止广播和受理，等待当前处理及发送排空。超时关闭传输并报错，不能谎报尚未退场的处理器已经结束。
- 新增仅服务一个方法的辅助逻辑优先局部函数；实现细节类型按唯一使用者嵌套。补写 XML 文档时保持同类成员完整并使用语义标签。

## 分帧与接收

| 分帧操作 | 实现约束 |
| --- | --- |
| GetLength | 返回包含包头的精确线协议长度，不改变报文，不分配完整编码副本 |
| Pack | 向 IBufferWriter<byte> 写入一帧，不 Flush；写入量等于声明长度 |
| Unpack | 数据不足返回 false 且输入不变；成功至少消费一个字节；非法输入抛出协议／数据异常 |
| Release | 每份成功解码的报文在处理完成或失败后的 finally 中释放一次，再归还输入；默认无操作 |

- 发送不取得调用者报文的所有权；不能根据 IDisposable 或装箱后的值类型推断释放责任。GetLength、Pack 和发送等待期间输入必须稳定，内存扩展发送也经过同一分帧流程。
- Receiver 独占 Read、Unpack、等待处理器、Release、AdvanceTo 和 Complete。处理器按连接顺序调用，不经 object 桥接、上下文字典、每包 Task.Run 或无界队列；不同连接可以并发调用同一处理器。每个 ValueTask 只消费一次，包括同步完成路径。
- Headed 先验证 UInt32 大端正文长度，再以受检运算计算含四字节包头的总长度。空正文消费包头；Headless 空输入返回不足，成功消费非空且有界的字节块。
- 单帧上限逐帧判断，残留上限只针对已解析到的未完成帧，不能用整个读取批次或尚未检查的后续完整帧判断超限。连续多帧沿原序列逐帧切片，不建立整批报文列表、拼接数组或任务数组。
- 帧数和线协议字节数预算控制连续同步处理量；至少允许一帧取得进展。归还前等待处理器结束，归还后不访问原切片；预算耗尽时保留尚未检查的数据，下一次读取继续已有完整帧，不等待新网络数据。已经异步让出时避免重复调度。
- 完整合法帧及大批次必须在背压下继续取得进展。接收实际暂停阈值至少为残留上限加一；阈值不等于进程内存上限，内核缓冲和一次底层读取另计，不能靠无界提高阈值或预租整批内存解决停滞。
- EOF 先处理完整帧，未完成尾帧按 IncompleteFrame 失败；正常半关闭允许已接收请求及响应完成，再有期限地排空发送。处理器故障只终止当前连接。批内前序处理不回滚，不承诺整批事务或合并 ACK。
- 外层包头包裹多个业务记录时，它仍是一个受单帧限额约束的报文；内部记录数、压缩展开大小和业务事务由协议层限制。

## 发送、广播与停止

- Sender 在等待写入权和编码之前预留操作数及完整线协议字节数，拒绝时立即返回 SendRejected；额度包含排队、编码、Flush 与尚未完成 Socket 发送的字节。通过公开传输计数观察发送进度，不反射 Pipe 内部状态。
- SemaphoreSlim 保护单写者，从 Pack 到 Flush 完成均持有写入权；不同调用者不承诺按调用时刻排序，需要顺序时由调用者逐次等待。FrameWriter 限制写入并核对声明长度，不用完整帧副本校验。
- 写入前取消归还预留；部分编码后失败或取消必须终止连接，避免后续帧接续损坏数据。关闭归还废弃额度，不把本地 Flush 成功当作实际字节已全部发送。
- FlushResult 的 IsCanceled、IsCompleted 与异常遵循一致失败语义；无竞争且同步完成时直接返回完成 ValueTask，需要等待才进入异步路径。停止排空以实际 Socket 发送计数达到目标为依据，不以 Flush 或 CompleteAsync 代替，不忙等。
- Broadcaster 独立限制全局广播接纳数与每次并发发送数，枚举实时连接集合，不为所有连接建立任务数组或全量副本。只将 SendRejected 计入 Rejected，其他发送错误计入 Failed；调用者取消传播。
- 停止先禁止广播和受理，再等待接入流程、当前处理与发送排空。期限届满关闭传输并报告失败；仍运行的处理器保留输入、登记和监测责任，不能提前报告完整退场。停止失败保持 Stopping，清理后允许重试。
- 接收、发送、受理、广播和监测后台任务均有明确所有者观察故障。重启创建新运行代次，不复用已取消的取消源或完成信号，不通过反射接管第三方收发循环。

## 共享监测与计量

- 按 TimeProvider 与 MaintenanceInterval 分组共享扫描，使用单调时间；最后连接退场后停止对应运行代次。每 256 条连接让出后刷新时间，代次完成串联避免并行扫描。
- IdleTimeout 基于实际接收进度，SendTimeout 基于发送停滞；FrameTimeout 从首次残留未完成帧开始，碎片持续到达不延长。监测采样实际收发计数，不能用调用 SendAsync 或收到业务 ACK 的时刻替代。
- 建连及显式短期限等待使用相应异步 API；正常收发避免为每次操作创建关联 CancellationTokenSource 或 Task.Delay，不用墙上时间判断超时。
- 通道字节来自完成的 Socket I/O，发送帧数来自成功 Flush；注销时累计到所有者。并发读取是近似快照，不为每次读取加全局锁。
- 客户端／服务端 Statistics 只读取所属时钟与周期组的已有监测计数，不触发扫描。组内存在启用诊断的连接才采集，未启用实例返回零；连接全部退出后仍能读取累计值。
- MaximumTimeoutDelay 从记录期限算起，包含批内等待，不包含关闭和业务退场。实际收发进度采样另有最多一个实际采样间隔的观测误差，扫描超期时可超过配置周期。MaximumScanDuration 包含分批让出的等待，OverrunCount 用于识别容量或调度不足。
- 诊断不添加设备、连接或端口等高基数标签，不按心跳创建日志字典或逐条记录。性能结论区分库分配、驱动与采样开销，分别解释内核缓冲、池保留和托管内存。

## Core 复用

- 跨段定长读数复用 `Zongsoft.Common.Buffer.TryGet*`；文本编码使用 `Encode`，在发送／广播等待完成后释放其所有者。只在需要独立保存借用报文时调用 `Lease`，明确移交及失败释放；`Owned` 不复制，也不延长原内存寿命。
- `Buffer` 不是 `MemoryPool<byte>` 或 `IBufferWriter<byte>`。保留 Pipe 自身内存池及限长写入装饰器，不能为复用引入全帧中转副本。接收文本直接使用支持序列的 BCL 解码入口。
- 生命周期、处理器和日志复用 [Core 通信生命周期与派发](../Zongsoft.Core/SKILL.md#通信生命周期与派发)。ChannelBase 拥有一次关闭／释放和共享完成结果；WorkerBase 拥有异步启停状态，Net 不叠加服务启停状态机。ListenerBase 派发处理器，实际流接收者拥有解码；注入处理器由提供者释放，无处理器也须执行取消检查及报文归还。HandlerBase 无附加参数时传递 null，显式 Parameters 保留身份。
- 共享维护器需要可注入时钟与代次完成等待；Common.Timer 的系统时钟和同步停止不满足该契约。通用 ObjectPool 的同步容量控制不替代异步发送接纳；不为复用引入错误的所有权或额外完整报文副本。
- 没有测量证据时，不另造 SocketAsyncEventArgs 引擎、对象池、线程调度器或 IValueTaskSource 状态机。共享生命周期等待使用可由多调用者等待的 Task。

## 多目标与依赖

- 三个目标共用收发、生命周期及时间抽象；条件编译只用于真实 API 差异的小范围实现。例如使用 System.Threading.Lock 时以 NET9_0_OR_GREATER 区分锁类型，不复制核心流程。
- 新增目标框架专属分支前核实 API 可用性与收益，为其他目标提供同语义实现；不为 JIT、GC 或 Socket 的运行时内部优化制造分支。
- 生产依赖与部署清单保持一致，BenchmarkDotNet 只属于性能工具。验证 runtimeconfig.json、deps.json 和实际运行，确保不引入 Microsoft.AspNetCore.App；只含目标 Runtime 的隔离运行验证与静态依赖检查分别记录。

实现依据：[Pipelines 读取、背压与调度](https://learn.microsoft.com/dotnet/standard/io/pipelines)、[SocketConnection](https://github.com/mgravell/Pipelines.Sockets.Unofficial/blob/551f244bb90364e137f58c79a260cb916a63192a/src/Pipelines.Sockets.Unofficial/SocketConnection.cs)、[接收与 ZeroLengthReads](https://github.com/mgravell/Pipelines.Sockets.Unofficial/blob/551f244bb90364e137f58c79a260cb916a63192a/src/Pipelines.Sockets.Unofficial/SocketConnection.Receive.cs)、[传输选项](https://github.com/mgravell/Pipelines.Sockets.Unofficial/blob/551f244bb90364e137f58c79a260cb916a63192a/src/Pipelines.Sockets.Unofficial/SocketConnection.Flags.cs)。

## 验证

- 按 [AGENTS.md](AGENTS.md#验证)执行构建和边界矩阵；[test](test) 使用公共 API、真实回环 Socket 和可控时钟，不为测试扩大生产可见性。
- 验证不读对端、并发发送、组包超时、滴入字节、半关闭、处理器内关闭、建连与释放竞争、广播、连接限额和关闭后的额度归还；通知变化覆盖顺序、仅一次、订阅者抛错、清理后通知、迟到故障及独立取消等待。
- [samples](samples/README.zh-Hans.md) 是功能演示；[benchmark](benchmark/README.zh-Hans.md) 承担微基准和分进程负载。性能结论必须说明负载、分配来源和实际容量。
- 保持双语文档、XML 契约、插件与部署描述一致。文件系统插件不自动启动 TCP 监听。

# Zongsoft.Messaging.ZeroMQ 重构评估报告

> 评估日期：2026-08-21；实施更新：2026-08-24<br />
> 评估范围：`Zongsoft.Core/src/Messaging`、`Zongsoft.Core/src/Communication`、`messaging/zero/src`、`messaging/zero/test`、范例及插件清单<br />
> 本报告保留重构前证据，并通过任务清单和“实施结果”记录当前修复状态；历史问题描述不应再被理解为现行实现。

## 实施任务清单

> 勾选项表示对应实现已经通过专项验证；未勾选项仍在实施或等待验证。

- [x] T01 建立测试研究、计划和状态记录，登记当前 16/17 基线及首发竞态证据。
- [x] T02 修复 Core 的并发订阅事务、失败回滚和竞争对象释放。
- [x] T03 建立 NetMQ Actor、共享异步初始化及发布完成语义。
- [x] T04 重构订阅分发、背压、主题转换和防御性协议解析。
- [x] T05 完成 Queue、Subscriber、Requester、Responder 和 Server 的生命周期治理。
- [x] T06 更新中英文 README、技能文档和重构报告。
- [x] T07 完成三目标构建和测试；hosting 因未部署本地插件未运行并已记录。
- [x] T08 汇总未解决事项及后续可靠性协议决策门。

### 评审纠偏清单（2026-08-24）

- [x] R01 恢复整文件重写时被误改的既有 C# 版权头。
- [x] R02 删除仅为测试注入引入的 `IZeroQueueTransport`，将实现收敛为 `ZeroQueue.Transport` 私有嵌套类。
- [x] R03 删除伪传输测试，改用真实 Server/Actor 路径验证负载快照、初始化失败重试、设置快照和有界背压。
- [x] R04 重新完成三目标构建、完整测试及文本格式校验。

## 1. 结论摘要

### 1.1 共同稳定化实施结果（2026-08-24）

本轮选择方案 A，继续提供瞬态、尽力而为的 PUB/SUB。现已完成共享异步初始化、Queue/Server Poller Actor、确定性订阅事务与关闭、本地发送完成语义、负载快照、有界顺序背压、逻辑/物理主题分离、防御性双帧解析，以及 Requester 的释放和可等待响应队列。Queue Actor 实现为 `ZeroQueue.Transport` 私有嵌套类；当前只有 NetMQ 一种生产传输，不额外设置传输接口或测试注入构造器。

`ProduceAsync` 成功只说明 Actor 已调用本地 Socket 发送；它不证明 TCP 已传递、订阅传播完成或 Handler 已执行。ZMQ-002 所述 slow joiner 窗口因此仍是所选语义的一部分，而不是通过固定延迟掩盖。

下列问题已在本轮修复：ZMQ-001、ZMQ-003～ZMQ-013、ZMQ-015、ZMQ-016，以及 ZMQ-020 的仓库元数据和确定性测试部分。ZMQ-002、ZMQ-014、ZMQ-017～ZMQ-019 保留为协议、恢复能力或部署安全的后续事项；未实现的 Core 选项继续通过 README 能力表明确说明。

当前插件已经形成完整的 Zongsoft 消息队列适配层：`ZeroQueueServer` 提供 XPUB/XSUB 交换与端点发现，`ZeroQueue`/`ZeroSubscriber` 实现发布订阅，`ZeroRequester`/`ZeroResponder` 实现请求响应，`ZeroQueueEventChannel` 对接事件交换器。基本构建和大多数集成场景可工作，但其生命周期、线程模型和投递语义尚未收敛为可以证明的状态机。

“初始化后延迟 100ms，首条消息仍偶发丢失”的直接原因已经定位：

1. 多个调用并发首次进入 `OnProduceAsync`；
2. 只有创建 `_publisher` 的调用从 `Initialize()` 得到 `true` 并等待 100ms；
3. 其余调用在初始化锁后看到 `_publisher` 已存在，`Initialize()` 返回 `false`；
4. 这些调用不等待，立即把消息加入 `NetMQQueue<Packet>`；
5. Poller 可在远端 XSUB 已收到相应订阅命令之前发送这些消息，ZeroMQ PUB/SUB 会静默丢弃它们。

因此，增大固定延迟只能改变复现概率，不能建立正确性。`PublisherSocket.HasOut` 与 `TrySignalOK()` 也只能说明本地套接字当前可写或已发送状态信号，不能证明目标主题的订阅已经从 XPUB 经 Proxy 传播到 XSUB/PUB 一侧。

建议先实施与最终可靠性选择无关的稳定化阶段：统一异步初始化、单线程 Socket Actor、确定性关闭、有界处理器调度、订阅失败回滚、逻辑/物理主题分离和协议防御。随后再选择以下投递目标之一：

- 继续使用瞬态、尽力而为的 PUB/SUB；
- 增加“订阅已传播”控制协议；
- 引入确认、重试和去重，提供端到端至少一次。

在投递目标确定前，不建议以继续增加延迟的方式合并首发修复。

## 2. 评估方法与基线

### 2.1 设计对照

重点对照了 Zongsoft.Core 的以下设计约束：

- `MessageQueueBase<TSubscriber>` 负责逻辑主题转换、每主题消费者缓存和发布/订阅模板方法；
- `MessageConsumerBase<TQueue>` 以 `ChannelBase` 表示可关闭的订阅通道；
- `IMessageQueueProvider` 与 `MessageQueueProviderBase` 从 `/Messaging/ConnectionSettings` 获取命名连接；
- `IRequester`、`IResponder`、`IRequest`、`IResponse` 和 `IRequestToken` 允许一个请求关联多个响应；
- `MessageEnqueueOptions` 和 `MessageSubscribeOptions` 定义跨适配器的能力契约，具体适配器应实现或明确说明未支持项。

同时对照了 NetMQ/ZeroMQ 的公开约束：

- NetMQ 明确说明 `NetMQSocket` 不是线程安全的，同一 Socket 不应被多个线程同时使用；Poller 用于把相关 Socket 操作归属到同一线程。[NetMQ Poller 文档](https://netmq.readthedocs.io/en/latest/poller/)
- XPUB/XSUB Proxy 会把订阅命令从订阅端转发到发布端，这是多发布者/多订阅者拓扑正常工作的前提。[NetMQ XPUB/XSUB 文档](https://netmq.readthedocs.io/en/latest/xpub-xsub/)
- PUB/SUB 面向瞬态分发，发送不阻塞；连接断开或高水位达到时消息可被静默丢弃。[ZeroMQ PUB/SUB RFC 29](https://rfc.zeromq.org/spec/29/)
- XPUB Welcome Message 只在订阅者订阅欢迎内容且 XPUB 正确处理传入订阅命令时发送；它证明订阅端连接，不证明某个独立 PUB 已收到业务主题订阅。[libzmq Socket 选项文档](https://libzmq.readthedocs.io/en/latest/zmq_setsockopt.html#zmq-xpub-welcome-msg-set-welcome-message-that-will-be-received-by-subscriber-when-connecting)

### 2.2 构建与测试基线

调研阶段在 Windows、.NET SDK 10.0.400 下完成以下验证：

```powershell
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net8.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net9.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net10.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net8.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net9.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net10.0

$env:ZONGSOFT_MESSAGING_TESTS = 'true'
dotnet test messaging\zero\test\Zongsoft.Messaging.ZeroMQ.Tests.csproj `
	-f net10.0 --blame-hang-timeout 2m
```

- ZeroMQ 源项目的三个目标框架均构建通过，0 警告、0 错误；作为前置依赖构建的 Core 在三个目标框架各有 4 个既有警告，均无错误；
- 调研阶段一次完整 net10.0 测试运行共 17 项，16 项通过，1 项失败；失败项为 `ZeroQueueConcurrencyTests.ConcurrentProduceOnSingleQueueIsThreadSafe`，20 秒内未收到全部 64 条并发首次发布消息；
- 本报告交付前再次运行完整套件为 17/17 通过，该并发用例耗时约 470ms；
- 随后对同一用例隔离重复 10 次，结果为 10/10 通过，单次约 527–622ms。历史完整套件失败与当前重复通过共同说明该问题具有明显的顺序和负载敏感性，重跑通过不能推翻已观测的失败证据。

集成测试仅在附加调试器或设置 `ZONGSOFT_MESSAGING_TESTS` 后执行，并且程序集禁用并行化、跨目标框架也串行运行。现有测试中广泛使用固定等待、重复发布和重试以规避 slow joiner，这些做法适合作为集成测试缓冲，但也会降低首发问题的可见性。

## 3. 重构前实现模型

### 3.1 服务端

`ZeroQueueServer` 包含三个网络端点：

| 端点 | Socket | 职责 |
| --- | --- | --- |
| 发现端点 | `ResponseSocket` | 返回 `Publisher=<port>;Subscriber=<port>`。 |
| 发布者入口 | `XSubscriberSocket` | 接收应用发布者的消息。 |
| 订阅者出口 | `XPublisherSocket` | 向应用订阅者分发消息和欢迎消息。 |

NetMQ `Proxy` 在 XSUB 与 XPUB 之间双向转发业务消息和订阅命令。发现端口默认是 `7969`；数据端口可固定或随机绑定。

### 3.2 客户端

`ZeroQueue` 第一次发布或订阅时同步执行端点发现，创建 `PublisherSocket`，把本地 `NetMQQueue<Packet>`、心跳计时器和订阅 Socket 加入同一个 `NetMQPoller`。发布调用将 `ReadOnlyMemory<byte>` 放入本地队列，由 Poller 回调执行实际 `SendMoreFrame(...).SendFrame(...)`。

`ZeroSubscriber` 在创建时订阅业务主题及欢迎消息；收到欢迎消息后完成 `SynchronizeAsync`。消息首帧包含主题、生产者实例和可选压缩参数，第二帧是二进制载荷。

### 3.3 主题与附加适配器

- `MessageQueueBase.GetTopic` 先解析默认主题；`ZeroQueue.GetTopic` 再添加 `Group:` 前缀；
- 生产者实例由 `Instance` 或 `Client` 生成，默认过滤掉本实例发布的消息；
- `ZeroRequester` 订阅 `<url>/reply`，请求和响应载荷均以请求标识及换行符开头；
- `ZeroQueueEventChannel` 使用 `Events/<qualified-name>` 主题；
- 压缩通过 `MessageEnqueueOptions.Properties["Compressive"]` 启用，线上帧标记为 `Compressor:Brotli`。

## 4. 重构前问题清单

严重级别定义：

- **P0**：会直接造成消息丢失、协议错误或不可控资源竞争，应在继续扩展前解决；
- **P1**：显著影响生命周期、可维护性、异常恢复或公开语义；
- **P2**：可观测性、性能、校验和工程质量问题，可随稳定化阶段处理。

### ZMQ-001 · 并发首次发布绕过等待（P0，已复现）

**证据**：`OnProduceAsync` 仅在 `Initialize()` 返回 `true` 时等待 100ms；初始化完成后的并发调用立即入队。完整测试套件曾在 64 条并发首次发布场景丢失消息。

**影响**：并发越高，越多首批消息绕过等待；调整等待时长无法保证正确性。

**建议**：把布尔初始化改为所有调用共享的异步初始化/就绪任务。所有首次调用必须等待同一状态转换；初始化失败要原子回滚并允许重试。

### ZMQ-002 · 没有可证明的发布就绪条件（P0，已验证）

**证据**：`HasOut && TrySignalOK()` 只检查本地发送条件；`SubscribeAsync` 等待的是 XPUB 欢迎消息。两者都没有确认业务主题订阅已经传播到发布者所连接的 XSUB 一侧。

**影响**：即使串行首发等待 100ms，也仍存在慢连接、调度抖动和远程网络下的消息丢失窗口。

**建议**：先定义目标语义。尽力而为模式只承诺状态清晰并在文档中说明窗口；订阅就绪模式需要由控制面跟踪主题订阅；端到端保证则需要消息确认。

### ZMQ-003 · Socket 线程归属不完整（P0，已验证）

**证据**：发布 Socket 在调用线程创建、连接和探测，随后由 Poller 回调发送；`Dispose` 又可能在任意调用线程释放它。源码中的 `ObjectDisposedException`/`TerminatingException` 捕获只能减轻症状，不能建立单线程所有权。

**影响**：并发初始化、发送和关闭时可能违反 NetMQ Socket 非线程安全约束，造成竞态、未定义异常或消息中断。

**建议**：使用专用 Actor/Poller 线程创建、连接、使用和销毁全部 Socket；外部线程只提交命令。关闭时先停止接收新命令，再在所有者线程排空或取消并释放资源。

### ZMQ-004 · Queue 释放不关闭活动订阅（P0，已验证）

**证据**：`ZeroQueue.Dispose(bool)` 释放 Poller、计时器、本地队列和发布 Socket，但没有遍历 `Subscribers` 关闭 `ZeroSubscriber`。Core 的 `MessageQueueBase.Dispose` 也不会代为处理。

**影响**：消费者集合、Handler 与订阅通道状态可能残留；关闭顺序依赖 Poller 被动释放 Socket，不能保证消费者收到关闭状态。

**建议**：在队列关闭流程中确定性关闭所有消费者，等待取消订阅完成，再停止 Poller 和释放 Socket；增加“带活动订阅释放 Queue”的回归测试。

### ZMQ-005 · 订阅失败会污染 Core 订阅缓存（P0，已验证，Core 层）

**证据**：`MessageQueueBase.SubscribeAsync` 先把新消费者加入字典，再调用 `OnSubscribeAsync`；该调用异常、取消或返回 `false` 时没有移除并释放消费者，也没有挂载 `Closed` 事件。

**影响**：后续同主题订阅会得到半初始化对象，无法恢复；并发竞争中创建但未加入字典的消费者也没有被释放。

**建议**：优先在 Core 模板方法中实现失败回滚和竞争失败释放，使所有适配器受益；ZeroMQ 不应通过局部延迟掩盖此问题。

### ZMQ-006 · 空载荷被错误当作心跳（P0，已验证）

**证据**：`string.IsNullOrEmpty(identifier) && data == null || data.Length == 0` 按运算符优先级等价于 `(匿名且 data 为 null) || data.Length == 0`，任何零长度业务载荷都会被丢弃；若 `data` 真为 `null`，后半部分还存在空引用风险。

**影响**：公开 API 允许零长度 `ReadOnlyMemory<byte>`，但接收端静默丢弃。

**建议**：先明确空业务消息是否有效，再将心跳判定写成完整括号条件并覆盖 `null`/空数组/匿名/具名四组测试。

### ZMQ-007 · `ProduceAsync` 完成语义和内存所有权不清（P0，已验证）

**证据**：方法把调用者提供的 `ReadOnlyMemory<byte>` 原样入队后立即返回，实际复制发生在 Poller 的 `Send` 中。调用者在返回后复用或修改底层数组，会改变尚未发送的内容；发送异常也无法反馈给原调用。

**影响**：方法名表现为异步发送，实际只完成本地排队；取消令牌在入队后不再生效。

**建议**：二选一并写入契约：入队前复制并明确“本地接受即完成”，或者为命令附加完成源，让调用者等待实际 Socket 发送结果。不要在定义所有权前单纯消除 `ToArray()`。

### ZMQ-008 · 无界且无序的 Handler 调度（P0，已验证）

**证据**：每条消息通过 `Task.Run` 启动独立任务；没有并发上限、顺序保证或背压，且忽略 `MessageSubscribeOptions.Reliability` 和 `FallbackBehavior`。

**影响**：突发流量会造成线程池和分配压力，同主题消息处理顺序不稳定；异常仅记录日志，无法应用重试/终止策略。

**建议**：引入有界 Channel/调度器，默认保证单订阅顺序，并允许显式并发；满载策略和 Handler 失败策略必须映射到公共选项或明确拒绝不支持值。

### ZMQ-009 · Group 破坏事件与请求响应主题（P0，代码推导，待专项测试）

**证据**：收到的 `Message.Topic` 是包含 Group 的物理主题。事件 Handler 只识别以 `Events` 开头的主题，因此 `Group:Events/...` 无法还原事件。响应器又以收到的物理请求主题构造 URL，响应时 `ZeroQueue.GetTopic` 再次添加 Group，可能形成 `Group:Group:.../reply`。

**影响**：普通发布订阅的 Group 可工作，但高层适配器在设置 Group 后可能静默失效。

**建议**：在 Queue 层明确逻辑主题与物理主题类型；Group 只在网络边界添加一次，接收后在交给高层适配器前移除。事件主题还应使用 `Events/` 边界匹配，避免误接收 `EventsX`。

### ZMQ-010 · 协议解析异常可能终止 Poller 回调（P1，代码推导）

**证据**：头帧解析、压缩器查找和解压在 `ReceiveReady` 回调中执行，缺少统一异常边界；帧数、头部长度、选项数量和解压后大小没有限制。

**影响**：畸形或不兼容发布者可导致异常逃逸、Poller 停止或内存放大。

**建议**：封装无异常解析器，限制帧和载荷大小，隔离解压异常并记录结构化诊断；未知协议版本或压缩器应按明确策略丢弃。

### ZMQ-011 · 请求器生命周期不完整（P1，已验证）

**证据**：`ZeroRequester` 持有 `MemoryCache`、事件订阅、令牌、订阅任务和 Queue 订阅，但类型不实现释放接口。替换 `Queue` 只清空任务缓存，不取消旧 Queue 的回复订阅。

**影响**：长生命周期或动态切换 Queue 时可能泄漏订阅和回调；待响应令牌无法统一取消。

**建议**：实现异步释放/关闭，禁止运行中无序替换 Queue，关闭时取消令牌、取消订阅、解除缓存事件并释放缓存。

### ZMQ-012 · 请求响应使用额外固定延迟和自旋（P1，已验证）

**证据**：`ZeroRequester` 在回复主题订阅后额外等待 100ms；`IRequestToken.GetResponses` 使用 1ms `SpinWait` 轮询 `ConcurrentBag`。

**影响**：仍不能建立订阅就绪保证，并产生不必要 CPU 消耗；`ConcurrentBag` 也不提供响应顺序。

**建议**：复用统一订阅就绪协议；响应存储改为可等待 Channel/队列，明确多响应顺序和完成条件。

### ZMQ-013 · 同步发现和重复超时轮询（P1，已验证）

**证据**：初始化在异步 API 内同步创建 `RequestSocket` 并使用 `SpinWait.SpinUntil`；内部 `TryReceiveFrameString(timeout, ...)` 已包含超时参数，外层又用相同超时轮询，注释与实际 API 语义不一致。

**影响**：占用调用线程，取消令牌无法及时中断发现；错误路径和总超时难以推理。

**建议**：把发现纳入 Actor 初始化状态机，使用单一截止时间和可取消的异步完成源；区分 DNS、连接、请求和协议解析错误。

### ZMQ-014 · Broker 随机端口重启后客户端不会重新发现（P1，已验证）

**证据**：客户端只在首次初始化时发现数据端口。NetMQ 可重连原端点，但服务端重启后若随机端口变化，现有客户端仍指向旧端点。

**影响**：开发环境固定端口重启测试会通过，但随机端口或配置变更后的恢复失败。

**建议**：当前版本文档要求部署固定数据端口；后续可通过连接监控触发重新发现、重建 Socket 和重订阅，并测试端口变化场景。

### ZMQ-015 · 设置对象可在初始化后修改（P1，已验证）

**证据**：`ZeroConnectionSettings` 作为公共可变对象保存在 Queue 中；Queue 只在构造时计算 `Instance` 和过滤器，却在初始化或重连时读取 Server、Port、Group 等设置。

**影响**：运行中修改设置会导致身份、过滤、主题和端点处于不一致版本。

**建议**：构造时生成不可变运行快照；配置热更新必须通过显式重建/重连状态转换完成。

### ZMQ-016 · 参数及标识边界校验不足（P2，已验证）

**证据**：服务端参数使用 `int.Parse`，未集中校验端口范围、入口出口相同等情况；`Math.Abs(Randomizer.GenerateInt32())` 在 `int.MinValue` 时溢出。

**影响**：无效配置产生低可诊断异常；极小概率实例生成失败。

**建议**：使用 `TryParse` 和明确的配置异常；校验 1–65535、端口冲突和地址；通过无符号格式化生成随机后缀。

### ZMQ-017 · 协议演进和兼容性信息不足（P2，已验证）

**证据**：协议版本只出现在 Welcome Message，业务帧没有版本或能力协商；头部通过换行和冒号分隔，但未限制主题/实例/选项中的保留字符。

**影响**：未来增加确认、控制帧或其他压缩算法时难以支持混合版本。

**建议**：若继续演进 v1，先定义保留字符、最大长度和未知选项规则；可靠性模式应使用可识别的新协议版本或独立端点，避免把旧客户端误当成可靠客户端。

### ZMQ-018 · 公共选项支持范围不透明（P2，已验证）

**证据**：除 `Compressive` 外，标签、延迟、过期、优先级、可靠性和订阅失败策略均未实现，但 API 会接受这些值。

**影响**：调用方可能误以为设置已经生效。

**建议**：README 保留能力矩阵；未来对不支持且非默认的选项选择显式抛错或记录告警，避免静默忽略。

### ZMQ-019 · 安全边界依赖部署环境（P2，已验证）

**证据**：服务端使用 `tcp://*` 绑定，当前未配置 ZAP/CURVE、访问控制或传输加密。

**影响**：端口暴露到不可信网络后，任意客户端可发现端点、发布消息或订阅数据。

**建议**：当前版明确限制在可信网络并用防火墙/网段隔离；如需跨信任边界，独立设计身份认证、密钥轮换、授权、审计和加密，不应只增加一个布尔开关。

### ZMQ-020 · 工程元数据与测试可见性（P2，已验证）

**证据**：项目 `RepositoryUrl` 仍指向已不存在的 `Zongsoft.Messaging.ZeroMQ` 根目录；集成测试默认静默跳过，固定延迟和重复发送可能掩盖问题。

**影响**：NuGet 源链接和维护入口不准确；普通 CI 可能显示测试通过但实际没有运行网络断言。

**建议**：修正仓库 URL；在 CI 增加明确的 ZeroMQ 集成测试作业和执行计数检查，将概率型压力测试与确定性状态机测试分开。

## 5. 重构方案比较

### 5.1 共同稳定化阶段

以下工作不依赖最终可靠性选择，应优先完成：

1. **Queue 状态机**：定义 `Created → Starting → Ready → Faulted → Disposing → Disposed`；所有发布和订阅共享一个异步初始化任务。
2. **Socket Actor**：专用线程创建、连接、监控、使用和销毁 Socket；外部 API 通过命令队列交互。
3. **确定性关闭**：拒绝新命令，取消或排空待发送命令，关闭消费者，停止 Poller，最后释放 Socket；为每一步设置上限。
4. **订阅事务**：Core 在字典添加、网络订阅和 Closed 事件挂载之间提供失败回滚；并发竞争失败的消费者立即释放。
5. **有界处理器调度**：每订阅默认保持顺序，设置并发上限、容量和满载策略；明确 Handler 失败与取消行为。
6. **主题模型**：同时保存逻辑主题和物理主题，Group 只在网络边界应用一次；事件/RPC 使用逻辑主题。
7. **协议防御**：集中解析与校验帧，隔离畸形消息和解压异常；明确空载荷与心跳。
8. **公开语义**：定义 `ProduceAsync` 完成点、内存所有权、取消窗口以及不支持选项的处理方式。

共同阶段可消除已知竞态并显著改善可测试性，但不能把 PUB/SUB 自动变为可靠队列。

### 5.2 方案 A：稳定的尽力而为 PUB/SUB

**目标**：保留现有协议和低延迟特性；不保证连接/订阅窗口内消息不丢失。

**实现**：完成共同稳定化；可使用 Socket Monitor 改善连接状态可观测性，但不把连接事件解释为订阅传播完成。`ProduceAsync` 可定义为“消息已被本地 Actor 接受”或“已调用 Socket 发送”，二者都不代表远端收到。

**兼容性**：最高，可保持现有两帧数据协议。

**代价**：中等。

**适用**：遥测、状态广播、可由后续数据覆盖的事件，以及允许偶发丢失的实时通知。

### 5.3 方案 B：订阅传播就绪

**目标**：订阅成功后，新发布者针对已存在订阅的第一条消息不因连接或订阅传播窗口丢失；仍不保证 Handler 已处理，也不提供持久化。

**关键约束**：普通 PUB Socket 无法读取订阅命令，因此仅在客户端等待连接事件不够。需要控制面掌握“主题订阅已经到达发布侧”的事实。

**可行设计**：

- 服务端显式读取 XPUB 订阅/取消订阅命令并维护带引用计数的主题注册表；
- 发布者使用独立控制通道注册实例并查询/等待指定主题的订阅代次；
- 订阅返回前，客户端等待服务端确认该订阅代次已转发到 XSUB；
- 发布者初始化后等待自己连接代次与目标主题订阅代次对齐；
- 定义“没有订阅者”时立即发送、等待、失败或允许丢弃中的一种策略；
- 服务端重启会推进代次，客户端必须重新发现、重连和重新订阅。

**兼容性**：数据帧可以保持 v1，但需要新增控制协议和混合版本降级规则。

**代价**：中高。

**限制**：只证明订阅传播/连接状态，不证明消息已经进入 SUB 队列或 Handler 完成。

### 5.4 方案 C：端到端至少一次

**目标**：调用方可知道消息是否被至少一个目标接收方确认；超时后重试，接收端按消息标识去重。

**可行设计**：

- 新增协议 v2，使用 ROUTER/DEALER 或明确的确认控制通道；
- 每条消息包含不可重复标识、发送者、主题、截止时间和协议版本；
- Broker 或消费者返回确认；生产者维护有界待确认窗口和退避重试；
- 消费者维护有时限的去重表，并定义 Handler 成功前确认还是成功后确认；
- 定义无消费者、部分消费者确认、广播确认集合、重试耗尽和毒消息策略；
- 若要求进程或 Broker 重启后仍不丢失，必须增加持久化日志，或者改用已有的持久化消息 Broker。

**兼容性**：低，建议使用新驱动名、独立端点或明确的协议协商，不要悄悄改变现有 `ZeroMQ` 队列语义。

**代价**：高；加上持久化后非常高。

**适用**：业务命令、资金或库存变更等不可接受静默丢失的场景。对于此类需求，优先评估成熟持久化 Broker 是否比自建协议更合适。

### 5.5 对比矩阵

| 维度 | 方案 A：尽力而为 | 方案 B：订阅就绪 | 方案 C：至少一次 |
| --- | :---: | :---: | :---: |
| 保持现有数据协议 | 是 | 基本可以 | 否/需版本化 |
| 消除当前并发初始化竞态 | 是 | 是 | 是 |
| 避免订阅传播导致的首发丢失 | 否 | 是 | 是 |
| 证明远端接收 | 否 | 否 | 是，取决于确认点 |
| 支持重试/去重 | 否 | 否 | 是 |
| 支持 Broker 重启后恢复 | 仅固定端点重连 | 需重建控制状态 | 需控制状态；持久化另议 |
| 实施风险 | 中 | 中高 | 高 |
| 与现有客户端兼容 | 高 | 中高 | 低 |

## 6. 推荐路线与决策门

### 阶段 0：文档和可重复证据（已完成）

- 保持 README 的能力矩阵和投递语义；
- 保留并发首发失败用例，并新增可控制订阅传播的确定性测试夹具；
- 在 CI 明确启用集成测试并验证实际执行数量。

### 阶段 1：稳定化（已完成）

- 实现共享初始化任务和 Socket Actor；
- 修复 Core 订阅失败回滚、Queue/Requester/Responder 释放；
- 修复空载荷、Group、异常边界和设置快照；
- 引入有界 Handler 调度；
- 定义 `ProduceAsync` 完成点和内存所有权。

### 决策门：投递目标

阶段 1 完成后，产品负责人必须从以下问题给出明确答案，才能进入协议实现：

1. “第一条不丢”是指订阅命令已传播、SUB Socket 已入队，还是业务 Handler 已成功？
2. 没有订阅者时发布应立即成功、等待订阅者、返回失败，还是丢弃？
3. 广播给多个订阅者时，需要任一确认、全部确认，还是指定数量确认？
4. Broker 或消费者重启后是否仍要求恢复未确认消息？
5. 是否接受新驱动名/新端点带来的兼容性切换？

若答案只要求降低偶发丢失且允许瞬态语义，选择方案 A。若要求“成功订阅后首发不受 slow joiner 影响”，选择方案 B。若要求业务处理可证明且可重试，选择方案 C 或成熟持久化 Broker。

## 7. 接口与兼容性影响

本轮没有改变消息双帧协议和主要方法签名，但存在以下公开语义变化：

- `ZeroRequester` 新增 `IDisposable` 和 `IAsyncDisposable`；
- `ProduceAsync` 改为复制负载并等待 Actor 调用本地 Socket 发送；
- 设置了 Group 时，`Message.Topic` 返回不含分组前缀的逻辑主题；
- Requester 首次订阅后禁止替换为不同 Queue；
- Queue 构造后对原设置对象的修改不再影响其运行快照。

后续可靠性方案可能产生以下影响：

| 变化 | 方案 A | 方案 B | 方案 C |
| --- | --- | --- | --- |
| `ZeroQueue` 内部状态机/Actor | 内部变化 | 内部变化 | 内部变化 |
| `ProduceAsync` 文档契约 | 必须明确 | 必须明确 | 可能扩展确认结果 |
| 新增就绪/健康状态接口 | 可选 | 建议 | 建议 |
| 控制协议 | 无 | 新增 | 新增 |
| 业务帧版本 | 保持 v1 | 可保持 v1 | 建议 v2 |
| 新配置项 | 容量/关闭超时 | 再加就绪超时/无订阅策略 | 再加确认、重试、去重和持久化策略 |

不要把可靠性模式塞入现有 `MessageReliability` 后静默改变行为；应先定义每个枚举值在 ZeroMQ 驱动中的精确含义和不支持值的失败方式。

## 8. 测试与验收建议

### 8.1 确定性测试

- 64 个并发调用共享同一初始化任务，初始化逻辑只执行一次；
- 在连接、订阅传播和允许发送三个状态点使用测试门闩，证明发送不会越过选定的就绪点；
- 初始化失败或取消后，所有等待者收到一致错误，下一次调用可重试；
- 同主题订阅失败后消费者不留在 `Subscribers`；并发竞争失败的消费者已释放；
- Queue 带活动订阅释放后，消费者关闭、Handler 置空、Poller 和 Socket 退出；
- 调用 `ProduceAsync` 返回后修改原数组，不影响已接受消息；
- 零长度业务载荷与匿名心跳分别按契约处理；
- Group 在普通发布订阅、请求响应和事件通道中只添加一次；
- 畸形头帧、未知压缩器、缺少载荷帧、多余帧和超大载荷不会终止 Poller；
- 有界调度器达到容量时按配置背压或拒绝，且同订阅默认保持顺序。

### 8.2 集成与故障测试

- 单发布者/单订阅者首发；单 Queue 并发首次发布；多发布者多订阅者扇出；
- 订阅与取消订阅和发布并发；
- Broker 在固定端口重启；Broker 在新端口重启并触发重新发现；
- 发布者或订阅者断网后重连；高水位达到；慢 Handler；
- net8.0、net9.0、net10.0 串行运行；
- 方案 B 应验证订阅代次和无订阅策略；方案 C 应验证确认丢失、重复消息、重试耗尽和去重过期。

### 8.3 宿主验证

代码重构后，将本地插件部署到隔离的 `D:\Zongsoft\hosting` 环境：

- daemon：验证插件自动启动、停止、端口占用失败和宿主关闭；
- terminal：验证命名 Queue、订阅/发布、重连和诊断命令；
- web：仅在需要验证事件交换或 Web 生命周期集成时运行。

宿主验证前应确认实际加载的是本地构建版本，不要用已安装 NuGet 版本的成功结果替代本地代码验证。

## 9. Agent 文档形式建议

本项目采用 `.agents/skills/zongsoft-zeromq/SKILL.md`，不新增模块级 `AGENTS.md`：

- ZeroMQ 知识只在创建、评审、排障或重构该适配器时需要，适合按描述显式或隐式激活；
- 仓库技能可封装架构事实、协议约束和验证入口，不会让所有仓库任务都加载大量模块细节；
- Codex 会从当前目录向仓库根扫描 `.agents/skills`，仓库根技能对从根目录启动的任务可见。[OpenAI Skills 文档](https://learn.chatgpt.com/docs/build-skills)
- `AGENTS.md` 更适合始终生效的仓库工作约定，并且只沿“项目根到当前工作目录”组成指令链；若从仓库根启动，位于 `messaging/zero` 的文件不会自动进入该链。[OpenAI AGENTS.md 文档](https://learn.chatgpt.com/docs/agent-configuration/agents-md)

技能应保持技术约束稳定，并链接本报告获取会持续变化的问题状态；问题修复后更新报告状态，而不是继续在技能中累积历史补丁说明。

## 10. 实施验证与遗留事项

### 10.1 最终验证

2026-08-24 在 Windows、.NET SDK 10.0.400 下完成：

- Core 和 ZeroMQ 源项目的 net8.0、net9.0、net10.0 构建全部成功；ZeroMQ 为 0 警告，Core 三个目标框架各保留 4 个与本次修改无关的既有警告；
- `MessageQueueBaseTest` 与 `MessageConsumerBaseTest` 在每个目标框架均为 8/8 通过；
- 评审纠偏前，设置 `ZONGSOFT_MESSAGING_TESTS=true` 后，ZeroMQ 完整套件在每个目标框架均为 42/42 通过；删除 6 个伪传输测试并增加 4 个真实 Server/Actor 行为测试后，net8.0、net9.0、net10.0 最终均为 40/40 通过；
- 原并发首发测试已改为“建立可观察路径后的网络突发”测试；Queue 初始化、负载快照和失败重试均经真实 Server/Actor 路径验证，不再为确定性门闩引入仅供测试使用的传输接口，也不再用远端首发必达断言错误描述尽力而为语义；
- 中英文 README 章节、配置名、示例和内部链接已对应；技能 YAML 前言保持可发现；本轮文本文件均为 CRLF。

`D:\Zongsoft\hosting` 当前未发现已经部署的 ZeroMQ 插件或配置。为避免修改宿主仓库或把已安装包误当成本地代码，本轮未运行 terminal/daemon/web；完成隔离部署授权后，按 8.3 节执行宿主冒烟。

### 10.2 明确保留的事项

- **ZMQ-002**：slow joiner 和订阅传播窗口仍可能丢失首条消息，这是方案 A 的既定尽力而为语义；
- **ZMQ-014**：Broker 使用随机数据端口重启后，客户端不会重新发现，生产环境继续要求固定数据端口；
- **ZMQ-017**：业务帧仍为 v1 双帧格式，没有版本协商、原始长度或端到端校验；
- **ZMQ-018**：除压缩外的延迟、过期、优先级、可靠性和回退选项仍未实现；
- **ZMQ-019**：认证、授权和传输加密仍依赖后续安全方案及部署边界；
- 超大载荷限制、随机端口重发现、真实断网、高水位跨进程压力和 hosting 生命周期需要后续专项环境验证。

### 10.3 下一决策门

只有在产品明确要求“订阅传播完成后首发不丢”时才进入方案 B（XPUB/控制面就绪）；只有在明确要求可确认、重试和去重时才进入方案 C（版本化至少一次协议）。在此之前不得重新引入固定延迟或把本地发送成功解释为远端接收。

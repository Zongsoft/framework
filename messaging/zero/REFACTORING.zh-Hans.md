# Zongsoft.Messaging.ZeroMQ 重构评估报告

> 评估日期：2026-08-21；实施更新：2026-08-25；协议设计更新：2026-08-25<br />
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

### Core 单注册表复核清单（2026-08-25）

- [x] S01 将 `MessageQueueBase` 的初始化缓存与 `Subscribers` 活动缓存合并为 `SubscriberCollection` 内的一份 Topic 注册表。
- [x] S02 保持 `Subscribers` 只展示活动消费者，并覆盖共享初始化、失败回滚、调用方取消、关闭和同 Topic 重订阅。
- [x] S03 审计 ZeroMQ 相似状态；确认 Actor Socket 索引和 Requester 所有权记录职责独立，不做错误合并。
- [x] S04 移除 `ZeroResponder._adapter` 冗余状态，并消除心跳主题对动态 `Count` 与枚举结果一致性的依赖。
- [x] S05 将 Core Communication/Messaging 遗漏的异常与警告日志，以及 Core 固定日志文本迁入中英文资源。
- [x] S06 完成 Core/ZeroMQ 三目标构建及完整测试。

### 2.0 协议设计清单（2026-08-25）

- [x] P10 确认采用方案 B，目标是订阅传播到实际发送 Socket 后再发送。
- [x] P11 确认不兼容 1.0 协议，不考虑新旧 Broker 或客户端混用。
- [x] P12 选择应用端 XPUB 直接观察订阅控制帧，不新增独立控制端口。
- [x] P13 完成 2.0 发现、Broker Epoch、Welcome、业务帧和发布等待状态机草案。
- [x] P14 完成配置、错误语义、故障恢复及测试验收设计。
- [x] P15 确认无订阅发布等待超时失败，并覆盖 Broker 随机端口重启恢复。
- [x] P16 设计评审通过，进入协议实现。
- [x] P17 横向审计 Core、RabbitMQ、Kafka、MQTT、Redis 和阿里云驱动的可靠性与确认边界。
- [x] P18 将 `MostOnce` 与 `LeastOnce` 拆分为 2A/2B 里程碑，并记录 Core 共性提取方案。
- [x] P19 确认 `LeastOnce` 纳入本次 2.0 重构，以发布时全部逻辑订阅为确认范围，并要求跨进程/Broker 重启持久化恢复。

完整设计见 [PROTOCOL-2.0.zh-Hans.md](PROTOCOL-2.0.zh-Hans.md)。本清单完成不表示 2.0 已实现，README 仍描述当前 1.0 行为。

## 1. 结论摘要

### 1.1 共同稳定化实施结果（2026-08-24）

本轮选择方案 A，继续提供瞬态、尽力而为的 PUB/SUB。现已完成共享异步初始化、Queue/Server Poller Actor、确定性订阅事务与关闭、本地发送完成语义、负载快照、有界顺序背压、逻辑/物理主题分离、防御性双帧解析，以及 Requester 的释放和可等待响应队列。Queue Actor 实现为 `ZeroQueue.Transport` 私有嵌套类；当前只有 NetMQ 一种生产传输，不额外设置传输接口或测试注入构造器。

`ProduceAsync` 成功只说明 Actor 已调用本地 Socket 发送；它不证明 TCP 已传递、订阅传播完成或 Handler 已执行。ZMQ-002 所述 slow joiner 窗口因此仍是所选语义的一部分，而不是通过固定延迟掩盖。

下列问题已在本轮修复：ZMQ-001、ZMQ-003～ZMQ-013、ZMQ-015、ZMQ-016，以及 ZMQ-020 的仓库元数据和确定性测试部分。ZMQ-002、ZMQ-014、ZMQ-017～ZMQ-019 保留为协议、恢复能力或部署安全的后续事项；未实现的 Core 选项继续通过 README 能力表明确说明。

当前插件已经形成完整的 Zongsoft 消息队列适配层：`ZeroQueueServer` 提供 XPUB/XSUB 交换与端点发现，`ZeroQueue`/`ZeroSubscriber` 实现发布订阅，`ZeroRequester`/`ZeroResponder` 实现请求响应，`ZeroQueueEventChannel` 对接事件交换器。基本构建和大多数集成场景可工作，但其生命周期、线程模型和投递语义尚未收敛为可以证明的状态机。

### 1.2 Core 单注册表与 ZeroMQ 状态复核（2026-08-25）

`MessageQueueBase` 原先用 `_subscriptions` 保存初始化任务，再把成功消费者转移到 `Subscribers` 的另一个字典。两份 Topic 索引表达同一订阅生命周期，转移需要先移除、后添加或先添加、后移除，无法形成单一原子状态。本次将注册表收进 `SubscriberCollection`：每个条目在同一对象内从 `Initializing` 转为 `Active` 或 `Removed`，并发调用共享其初始化任务；公共 `Count`、索引和枚举只投影 `Active` 条目。失败、共享初始化异常、单调用方取消、Queue 关闭和 Consumer 关闭均按“Topic + 条目实例”回滚，旧条目不能删除同 Topic 的新重试条目。

对 ZeroMQ 的相似字段审计如下：

| 状态组合 | 结论 | 处理 |
| --- | --- | --- |
| `ZeroQueue._initialization` / Transport `_publisher` | 前者是共享启动事务，后者是 Actor 所有的 Socket 资源，不是重复权威状态。 | 保留隔离，调用方取消不取消共享启动。 |
| Core `Subscribers` / Transport `_subscribers` | 前者是成功逻辑订阅视图，后者是 Poller Socket 所有权索引；初始化期间允许只存在于后者。 | 不合并，维持线程归属边界。 |
| `ZeroRequester._subscriptions` / Queue `Subscribers` | 前者记录 Requester 所有权并等待在途订阅以便确定性释放，后者是 Queue 全局活动视图。 | 不合并，避免 Dispose 漏掉初始化中的响应订阅。 |
| `ZeroSubscriber._channel` / Transport Socket 值 | Subscriber 负责 Attach/Detach，Transport 负责 Poller 注册与销毁次序。 | 不合并，避免跨线程读取替代 Actor 索引。 |
| `ZeroResponder._adapter` / Subscriber.Handler | `_adapter` 仅写入和清空，从未作为状态读取。 | 删除冗余字段。 |

此外，心跳主题原先先读取活动视图的动态 `Count` 分配数组，再二次枚举；并发激活可能使枚举项多于数组长度。本次改为单次枚举收集后生成数组，不再要求两个时刻的视图一致。

本次同时复核了直接相关的 Core Communication/Messaging 异常与日志文本，并扫描 Core 的显式日志调用：5 条异常消息、1 条消息队列警告和 1 条重复固定错误日志已迁入默认及简体中文资源。该复核针对本次消息通信改动及日志入口，不把 Core 其他子系统历年来的内部参数校验文本机械纳入 ZeroMQ 重构范围。

最终验证结果：Core 消息测试在 net8.0、net9.0、net10.0 各 10/10 通过；ZeroMQ 三目标构建均为 0 警告、0 错误；启用 `ZONGSOFT_MESSAGING_TESTS=true` 后，完整集成套件在三个目标框架各 40/40 通过。

### 1.3 方案 B 设计状态（2026-08-25）

产品已确认下一阶段采用订阅传播就绪方案，并明确不兼容 1.0、不考虑新旧客户端混用。设计选定应用发布端 XPUB 直接观察订阅控制帧：只有匹配物理主题的订阅已经到达实际发送 Socket，Actor 才释放业务发布命令。该方案不新增控制端口，仍不提供远端接收或 Handler 确认。

2.0 设计草案已经覆盖发现协议、Broker Epoch、Welcome Message、业务帧版本、按主题有界等待、超时取消、断线失效、重连恢复和三目标测试矩阵，详见 [PROTOCOL-2.0.zh-Hans.md](PROTOCOL-2.0.zh-Hans.md)。当前仍是设计阶段，运行时代码和 README 尚未切换到 2.0。

### 1.4 可靠性与跨驱动复核（2026-08-25）

`MessageQueueBase` 已统一生产/订阅入口和订阅事务，`MessageConsumerBase` 保存 Handler 与订阅选项，`Message` 已提供传输无关的显式确认回调。不过 Core 尚未统一归一化可靠性默认值、声明驱动生产/订阅能力，也未处理同 Topic 的后续订阅请求携带不同 Handler、Tags 或可靠性选项时的冲突。

横向结果表明，确认的载体必须留在驱动：RabbitMQ 使用 Delivery Tag 确认，Kafka 提交消费位置，Redis Streams 执行 `XACK`，阿里云队列支持带延迟确认，MQTT 把可靠性映射到 QoS。多数驱动当前还会忽略一部分 `MessageEnqueueOptions` 或 `MessageSubscribeOptions`，MQTT 甚至在选项为 `null` 时使用 AtLeastOnce，与 Core 的 `MostOnce` 默认值不一致。这不是 ZeroMQ 层应复制解决的问题。

因此建议把可靠性支持纳入同一个 2.0 重构总体目标，但拆成两个独立验收里程碑：2A 只实现 `MostOnce` 的订阅传播就绪；2B 才实现 `LeastOnce` 的确认和重投。`ExactlyOnce` 在 ZeroMQ 生产和订阅入口始终显式拒绝，不改变其他驱动已有的能力映射。2A 完成而 2B 尚未完成期间，`LeastOnce` 也必须显式失败，不能静默降级。

Core 的后续公共改动限定为：在 `MessageQueueBase` 统一选项入口和本地化能力校验、解决重复订阅选项一致性，并保留驱动可覆写的能力/验证钩子。ACK 令牌、待确认窗口、重投、持久化和 Socket 状态仍由具体驱动实现；在至少两个驱动出现相同且语义一致的代码之前，不为 ZeroMQ 单独把协议状态机上提到 Core。

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

**关键约束**：普通 PUB Socket 无法读取订阅命令，因此仅等待连接事件不够。就绪状态必须在实际发送业务消息的 Socket 上可观察，不能通过另一条连接上的 Broker 确认间接推断。

**选定设计（2026-08-25）**：

- 应用发布端由 PUB 改为 XPUB，在 Queue Actor 线程直接接收 Broker XSUB 转发的订阅/取消订阅帧；
- 发布命令仅在该 XPUB 已观察到匹配物理主题前缀后发送，否则进入按主题有界 FIFO；
- 发现响应和 Welcome Message 携带 Broker Epoch，断线或 Epoch 变化立即清空旧订阅状态；
- 无订阅命令按待确认策略等待或失败，不使用固定延迟；
- 不新增控制端口，也不维护订阅者身份或确认集合；
- 发现、Welcome 和业务头统一升级为 2.0，不兼容 1.0，也不提供降级分支。

详细帧格式、状态机、配置及验收规则见 [2.0 可靠性协议设计](PROTOCOL-2.0.zh-Hans.md)。

**兼容性**：产品已明确不兼容 1.0，不考虑新旧客户端混用；2.0 客户端只接受 2.0 Broker。

**代价**：中高。

**限制**：只证明订阅传播/连接状态，不证明消息已经进入 SUB 队列或 Handler 完成。

### 5.4 方案 C：端到端至少一次（建议作为 2B）

**目标**：调用方可知道消息是否达到约定的接收方确认范围；超时后使用同一消息标识重投，公开允许重复投递。

**可行设计**：

- 新增协议 v2，使用 ROUTER/DEALER 或明确的确认控制通道；
- 每条消息包含不可重复标识、发送者、主题、截止时间和协议版本；
- Broker 或消费者返回确认；生产者维护有界待确认窗口和退避重试；
- `Message.AcknowledgeAsync` 绑定驱动确认回调，沿用其他消息驱动的显式确认范式；
- 重投可能再次进入 Handler，业务处理器负责必要的幂等；有限去重只能作为优化，不能宣称 `ExactlyOnce`；
- 定义无消费者、部分消费者确认、广播确认集合、重试耗尽和毒消息策略；
- 若要求进程或 Broker 重启后仍不丢失，必须增加持久化日志，或者改用已有的持久化消息 Broker。

**兼容性**：低，建议使用新驱动名、独立端点或明确的协议协商，不要悄悄改变现有 `ZeroMQ` 队列语义。

**代价**：高；加上持久化后非常高。

**适用**：业务命令、资金或库存变更等不可接受静默丢失的场景。对于此类需求，优先评估成熟持久化 Broker 是否比自建协议更合适。

### 5.5 对比矩阵

| 维度 | 方案 A：尽力而为 | 方案 B：订阅就绪 | 方案 C：至少一次 |
| --- | :---: | :---: | :---: |
| 保持现有数据协议 | 是 | 否，选定设计统一升级 2.0 | 否/需版本化 |
| 消除当前并发初始化竞态 | 是 | 是 | 是 |
| 避免订阅传播导致的首发丢失 | 否 | 是 | 是 |
| 证明远端接收 | 否 | 否 | 是，取决于确认点 |
| 支持重试/允许重复投递 | 否 | 否 | 是 |
| 支持 Broker 重启后恢复 | 仅固定端点重连 | 需重建控制状态 | 需控制状态；持久化另议 |
| 实施风险 | 中 | 中高 | 高 |
| 与现有客户端兼容 | 高 | 不考虑 | 低 |

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

### 决策门：可靠性阶段

2026-08-25 已确认选择方案 B，并明确不兼容 1.0、不会出现新旧客户端混用。设计进一步选定“订阅控制帧到达实际发送业务消息的应用 XPUB”为 `MostOnce` 就绪点；不等待 SUB 入队或 Handler 成功。

进入实现前仍需确认：

1. 没有匹配订阅时，是否采用“等待 `ReadinessTimeout`，超时失败”的推荐策略；
2. 是否在 2A 同时实现 Broker 随机数据端口变化后的自动重新发现、Socket 重建和重订阅；
3. 是否接受将 `LeastOnce` 纳入同一次 2.0 重构，但作为 2B 独立验收；
4. `LeastOnce` 是确认发布时的全部逻辑订阅，还是任意一个消费者；
5. `LeastOnce` 是否要求跨发布者、Broker 或订阅者进程重启保证。若要求，2B 必须包含持久化待确认日志。

2A 的多订阅者只要求“至少一个匹配订阅已传播”，不增加消息确认、重试或持久化。这个条件不能直接套用到 2B 的确认范围。完整待确认项见协议设计的 D04、D06、D09 和第 13 节。

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
| 订阅就绪机制 | 无 | 应用 XPUB 直接观察 | 确认控制协议 |
| 业务帧版本 | 保持 v1 | 统一升级 v2 | 复用并扩展 v2 |
| 新配置项 | 容量/关闭超时 | 再加就绪超时/无订阅策略 | 再加确认、重试、耗尽和持久化策略 |

Core 先在 `MessageQueueBase` 统一选项归一化、驱动能力校验及重复订阅选项一致性；ZeroMQ 再实现具体映射。`MostOnce` 在 2A 完成，`LeastOnce` 在 2B 完成，`ExactlyOnce` 始终不支持。任何尚未完成的值都必须显式失败，不能降级或静默忽略。

## 8. 测试与验收建议

### 8.1 确定性测试

- Core 的所有生产/订阅重载进入同一可靠性校验；驱动能力不支持时在创建 Socket 或缓存订阅前失败；
- 同 Topic 的重复订阅若 Handler、Tags 或可靠性选项不一致，不得静默返回与请求不符的既有消费者；
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
- 2A 应验证订阅代次和无订阅策略；2B 应验证确认丢失、重复消息、重试耗尽、确认范围和已承诺的持久化故障边界。

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

- **ZMQ-002（已修复）**：2.0 应用 XPUB 观察订阅控制帧后才释放首发消息；确定性首发与 64 路并发测试不再依赖延迟或重复发送；
- **ZMQ-014（已修复）**：Broker Epoch、Socket Monitor 和周期发现共同支持固定或随机运行端口重建与重订阅；
- **ZMQ-017（已修复）**：发现、Welcome 和业务头均严格要求 2.0，帧数、选项、压缩器及大小执行防御性校验；
- **ZMQ-018（部分保留）**：已实现 `MostOnce` 和 `LeastOnce`，`ExactlyOnce` 明确不支持；延迟、优先级和订阅回退策略仍未实现；
- **ZMQ-019**：认证、授权和传输加密仍依赖后续安全方案及部署边界；
- 持久化操作通过容量 1024 的单读者串行执行器运行，避免逐消息 `Task.Run` 并保证“先落盘、后投递”的顺序；Broker Actor 在关键落盘点等待结果，因此高延迟远程存储仍需专项压测；
- 真实断网、高水位跨进程压力和 hosting 生命周期仍需要后续专项环境验证。

### 10.3 下一决策门

2.0 的产品决策门已经关闭，2A 与 2B 运行时均已实现。剩余门槛是 P27/P28/P32 的三目标回归、文档一致性、CRLF/Tab 校验和可用宿主冒烟；在这些验收完成前不把本节的历史基线替换为最终证据。

## 11. 2.0 实施结果（2026-08-25）

- Core：`MessageQueueBase` 用单一可靠性上限统一校验全部生产和订阅入口，并以唯一 `Subscribers` 注册表共享并发初始化；Core 提供传输无关的 `IMessageStorage`、`MessageStorageBase`，不解释 ZeroMQ 会话或 ACK 数据。
- Broadcast：发现响应和 Welcome 携带 Broker Epoch；内部 `ZeroBroadcast` 使用 XPUB 观察真实订阅传播，无订阅按 `ReadinessTimeout` 失败，等待队列有界且同主题 FIFO；内部 `ZeroBroadcastServer` 管理 XSUB/XPUB 双向转发，断线后支持随机端口重新发现。
- Control：ROUTER/DEALER 控制端点登记稳定逻辑订阅，Broker 在接受发布时快照全部匹配目标；`Message.AcknowledgeAsync` 显式确认，未确认目标使用同一标识重投。Requester/Responder 仍保持独立的请求应答语义，不复用可靠 Control。
- 持久化：发布端发送前把完整 `Message` 写入外部 Storage，Broker 投递前持久化目标与确认集合；Pending→Terminal 先写后删。Broker、发布者或订阅者重启后按相同消息/订阅身份恢复，过期消息进入终止分区。
- 存储解耦：ZeroMQ 不提供默认文件实现，也不拥有外部 Storage 生命周期。Queue 与 Server 通过 `Storage` 属性或命名配置挂载独立存储插件；Broker 无 Storage 时仅启动 Broadcast 并返回 `Control:0`。
- 最终三目标证据：Core Messaging 聚焦测试在 net8.0、net9.0、net10.0 各 14/14；ZeroMQ 完整套件各 62/62；2B 专项测试覆盖显式确认、同标识重投、全目标确认、Broker 重启、发布者本地日志重建、订阅者身份恢复、新订阅不重放、身份冲突、实例过滤、就绪超时和过期终止。

## 12. ZeroMQ 语义拆分与消息存储解耦实施清单

- [x] N01 登记本轮决策、当前 62/62 ZeroMQ 基线、工作树已有修改和新任务映射。
- [x] N02 重构 Core 可靠性上限，删除枚举定义校验和双 `Supports*`，完成所有消息驱动的能力声明与 MQTT 默认值统一。
- [x] N03 新增 `IMessageStorage` 和 `MessageStorageBase`，删除 `IMessageStore`、`MessageStoreBase`、`MessageStoreEntry` 及失效资源。
- [x] N04 为 ZeroMQ/MQTT 增加 Storage 属性和命名解析入口，删除 `ZeroMessageStore`、`StoragePath`、默认文件存储及所有权逻辑。
- [x] N05 提取内部 `ZeroBroadcast`，迁移 XPUB 发布、订阅传播就绪、按主题有界 FIFO、心跳、SubscriberSocket、暂停恢复和重连状态。
- [x] N06 提取内部 `ZeroBroadcastServer`，迁移 XSUB/XPUB 绑定、Welcome、双向转发和 Broadcast Socket 生命周期。
- [x] N07 将 `ZeroQueue.Transport` 和 `ZeroQueueServer.ServerAgent` 收敛为纯 `LeastOnce`/Control 实现，删除两个 `.Reliability.cs` 分部文件。
- [x] N08 用 `Message` 重写 ZeroMQ 发布端与 Broker 持久化映射，保持目标快照、ACK 集合、重投次数和过期状态为 ZeroMQ 私有载荷。
- [x] N09 完成 Queue/Server 组合生命周期：单一 Subscribers 注册表、共享 Poller 线程、Broadcast 与 Control 分别启停、异常回滚及确定性释放。
- [x] N10 保持 `ZeroRequester`、`ZeroResponder` 行为不变，完成请求应答、事件通道和 Group 语义回归。
- [x] N11 同步 README、协议、重构报告、技能文档、配置示例和本地化资源，明确 Broadcast、LeastOnce、Storage 与 Control 边界。
- [x] N12 完成三目标构建、完整测试、格式校验和可用宿主冒烟，并登记四种存储插件的后续里程碑。

### 12.1 N01 证据

- 2026-08-25 在现有工作树上设置 `ZONGSOFT_MESSAGING_TESTS=true`，运行 ZeroMQ net10.0 完整套件，结果 62/62 通过。
- 工作树包含上一阶段的 Core、ZeroMQ、文档、资源和测试修改，以及尚未跟踪的 2.0 可靠性实现；本轮在其上继续，不回退项目文件或部署清单。
- 本轮不兼容旧存储类型、旧持久化文件或旧协议；`ExactlyOnce` 能力由驱动声明，ZeroMQ 上限为 `LeastOnce`。

### 12.2 N02 证据

- `MessageQueueBaseTest` net10.0 为 13/13，通过生产与订阅全部带选项入口、上限内低值/等值以及超上限拒绝。
- ZeroMQ、MQTT、RabbitMQ、Kafka、Redis Streams、阿里云 MNS 的 net10.0 构建均通过；MQTT 空选项映射为 `MostOnce`。
- Core 不再调用 `Enum.IsDefined`，也不再区分生产与订阅的 `Supports*` 能力。

### 12.3 N03 证据

- `MessageStorageBaseTest` 验证存储区名、完整 `Message` 字段、TTL、参数校验、取消和异步枚举；与队列基类测试合计 net10.0 15/15 通过。
- Core 已不存在 `IMessageStore`、`MessageStoreBase`、`MessageStoreEntry` 或对应失效枚举校验资源。

### 12.4 N04 证据

- `ZeroQueue`、`ZeroQueueServer`、`MqttQueue` 和 `MqttQueueServer` 均通过公共 `Storage` 属性接入外部 `IMessageStorage`；命名配置由队列基类使用现有服务提供程序解析。
- ZeroMQ 不再包含 `ZeroMessageStore`、`StoragePath`、默认目录或外部 Storage 的释放逻辑；Broker 未配置 Storage 时仅启动 Broadcast，发现响应返回 `Control:0`。
- ZeroMQ 可靠性与 Server 聚焦测试 net10.0 为 18/18，MQTT Storage 状态测试为 2/2。转储定位并修复了可靠订阅错误依赖客户端 Storage 的问题：订阅者可以连接 Control，只有 `LeastOnce` 发布端强制要求 Storage。

### 12.5 N05 证据

- 内部 `ZeroBroadcast` 与 Transport 共享唯一 Poller，集中管理应用 XPUB、SubscriberSocket、订阅传播、按主题有界 FIFO、心跳、单订阅暂停恢复和 Broadcast 重连状态。
- `ZeroBroadcast` 只保存物理 Socket 与待发送状态，不维护第二份 Core 逻辑 Topic 注册表；逻辑订阅仍以 `MessageQueueBase.Subscribers` 为准。
- ZeroMQ 发布、订阅与并发聚焦测试 net10.0 为 22/22，包含首条及并发发布、容量、FIFO、超时、取消、背压、Group、固定/随机端口重连。

### 12.6 N06 证据

- 内部 `ZeroBroadcastServer` 与 `ServerAgent` 共享唯一 Poller，集中管理 XSUB/XPUB 的绑定、Welcome、双向转发和 Broadcast Socket 释放。
- `ServerAgent` 保留发现 REP 与可靠 Control；发现响应从 Broadcast 子系统读取 Ingress/Egress，从 Control 子系统读取 Control，故障和端口状态彼此独立。
- Server、发布、订阅与并发聚焦测试 net10.0 为 26/26；Broker 无 Storage、固定/随机数据端口重启和 Broadcast 发布订阅均通过。

### 12.7 N07/N08 证据

- 原 `ZeroQueue.Transport.Reliability.cs` 和 `ZeroQueueServer.Reliability.cs` 已删除；可靠通道代码改以 `Control` 命名，Transport/ServerAgent 负责发现与 `LeastOnce` Control，Broadcast Socket 细节分别留在两个内部组件。
- 发布端和 Broker 直接把完整 `Message` 写入 `IMessageStorage`。外层保存 Identifier、Topic、Identity、Tags、Timestamp；客户端私有载荷只含业务 Data、绝对过期和 Readiness 截止，Broker 私有载荷只含 Data、过期、目标快照、ACK 集合、重投次数和状态。
- Control 的 PUBLISH/DELIVER 同步传递 Tags 与首次产生 Timestamp，订阅者恢复出的 `Message` 同时保留生产者 Identity；不保留旧帧兼容分支。
- ZeroMQ 源项目 net10.0 构建为 0 警告，可靠性与 Server 聚焦测试为 19/19，包含端到端元数据、显式 ACK、同标识重投、发布者/Broker 恢复和 Terminal 优先恢复。

### 12.8 N09/N10 证据

- Queue 与 Server 各自只有一个 Actor/Poller；Broadcast 与 Control 分别启停、分别释放 Socket，外部 Storage 不由驱动释放。Transport 初始化仍由所有调用共享，失败后可在同一 Actor 上重新发现。
- 同一 Queue 可同时持有 Broadcast 与 Control 订阅，两者共同使用 `MessageQueueBase.Subscribers` 唯一注册表；关闭订阅、Queue 或 Server 时按“订阅处理器 → Broadcast/Control Socket → Actor → 持久化执行器”的顺序确定性退出。
- ZeroMQ net10.0 完整套件最终为 68/68；Requester、Responder、EventChannel 与 Group 聚焦回归为 14/14。默认事件通道在 Queue 和 Server 均无 Storage 时实际经 Broadcast 投递，未访问 Control。

### 12.9 N11 证据

- README 中英文的概述、配置、使用、能力矩阵、语义和排障章节一一对应；删除 `StoragePath`、默认文件日志、`ZeroMessageStore` 和旧 Store API，增加命名/直接 Storage 挂载与 `Control:0` 行为。
- 2.0 协议记录内部 Broadcast、独立 LeastOnce Control、完整 Message 元数据帧、Pending→Terminal 顺序和无旧帧兼容分支；技能文档同步当前文件与职责边界。
- Core、ZeroMQ、MQTT 的默认与简体中文资源键分别为 133/133、39/39、18/18，全部对齐；新增用户异常均来自资源。按既定要求未为本地化文本新增单元测试。
- ZeroMQ 与 MQTT 源项目 net10.0 构建均为 0 警告，旧 `IMessageStore`、`ZeroMessageStore`、`StoragePath` 和 `.Reliability.cs` 只在报告的删除说明中出现。

### 12.10 N12 最终验收

- Core 与 ZeroMQ 的 net8.0、net9.0、net10.0 构建全部成功；ZeroMQ 均为 0 警告，Core net8/net9 各保留 4 个与本次无关的既有安全模块警告。
- Core Messaging 聚焦测试三目标各 15/15；设置 `ZONGSOFT_MESSAGING_TESTS=true` 后，ZeroMQ 完整套件三目标各 68/68，MQTT 完整套件三目标各 18/18。
- RabbitMQ、Kafka、Redis Streams、阿里云 MNS 的全部目标框架构建均为 0 警告；MQTT 显式 `ExactlyOnce` 映射 QoS2，空选项 `MostOnce` 映射 QoS0。
- 所有本轮文本均为 CRLF，手写 C# 缩进为 Tab；资源键、README 本地链接、旧符号扫描和 `git diff --check` 通过，既有文件头版权文本未被改动。
- `D:\Zongsoft\hosting` 只读审计发现 `packages` 中的 `Zongsoft.Messaging.ZeroMQ@1.8.1`，不是本地构建；按“不复制、不部署”约束未运行会加载旧包的 terminal/daemon/web。`zongsoft.pod-redis.yaml` 已确认存在，但测试内存 Storage 已完整验证可靠流程，未无必要启动 Podman。

### 12.11 独立 Storage 插件后续里程碑

1. **SQLite（优先）**：定义 Message 列映射、WAL/同步级别、主键与分区名、TTL 清理、单机多进程锁和崩溃恢复测试，适合作为嵌入式耐久存储。
2. **Redis（优先）**：定义 Hash/Sorted Set 或 Stream 布局、Lua 幂等更新、TTL、AOF/RDB 耐久等级、连接池与背压；使用 hosting 的 Podman Redis 做故障恢复测试。
3. **DuckDB（受限）**：先明确其分析型、单写者特征，不默认用于消息热路径；仅在批量归档/审计场景评估分区、检查点和写入并发。
4. **etcd（受限）**：明确值大小、写吞吐和配额限制，优先用于小型控制状态而非大消息载荷；评估事务、租约 TTL、压缩和 watch 恢复。

四个项目都只实现 Core `IMessageStorage`，不得引用 ZeroMQ、MQTT 或其他队列驱动；插件容器负责实例命名和生命周期。SQLite/Redis 完成契约与耐久测试后再进入实现，DuckDB/etcd 必须先通过适用性决策门。

## 13. 投递契约简化与 Control 组件化

本节取代 12.2～12.10 中关于“等待订阅就绪、发布端持久化、目标快照和全部目标确认”的实施结论；前节保留为决策演进记录，不再表示当前运行契约。

- [x] S01 登记新的 MostOnce/LeastOnce 完成契约和当前测试基线。
- [x] S02 补齐 `IMessageStorage` 与 `MessageStorageBase` XML 文档，保留 `name` 参数。
- [x] S03 删除 ZeroMQ 客户端 Storage、ReadinessTimeout、PendingCapacity 和相关状态。
- [x] S04 简化 `ZeroBroadcast` 为即时订阅检测和一次发送，并贯通消息唯一标识。
- [x] S05 提取客户端 `ZeroControl`，消除 Transport 中的 Reliability 后缀方法。
- [x] S06 提取服务端 `ZeroControlServer`，实现 Broker 持久接纳和竞争消费。
- [x] S07 删除发布者日志、Terminal、全部目标确认和未来订阅等待协议。
- [x] S08 更新协议、本地化资源、中英文 README、技能文档和重构报告。
- [x] S09 完成三目标构建、完整测试、格式检查和旧符号扫描。

本阶段把 `ProduceAsync` 恢复为 Core 已声明的结果契约：成功返回唯一消息标识，无可投递订阅返回空。`MostOnce` 只在发送瞬间检查 XPUB 已知订阅并本地发送一次；`LeastOnce` 在 Broker 持久接纳后即完成生产调用，由 Broker 在后台竞争投递并重试到任一显式 ACK。发布端不再持久化或恢复消息，Broker 不再维护 Terminal、全部目标快照或 ACK 集合。

S03～S07 已由 net10.0 聚焦测试 23/23 及完整测试 65/65 验证。客户端不再暴露或解析 Storage；Broadcast 无等待队列，在无匹配订阅时不发送并返回空；每次成功发布的 Identifier 会进入广播帧或 Control 帧。`ZeroControl` 与 `ZeroControlServer` 分别拥有 DEALER/ROUTER 状态，但共享外层 Poller。Broker 只保存 Pending，按主题轮询选择一个在线订阅者，任一有效 ACK 删除记录；超时、取消或断线造成的接纳结果不确定仍向发布者抛出异常。测试进一步证明：Storage 写入已经开始后，客户端超时或取消并不能撤销 Broker 随后完成的持久接纳；畸形 Control 命令也不会终止 Broker Poller。

S08 已同步中英文 README、2.0 协议、仓库技能和本报告：客户端配置删除 Storage、ReadinessTimeout 和 PendingCapacity；用户文档明确说明两种 `null` 结果、成功标识的边界、Broker-only Storage、竞争消费与业务幂等要求。Control 超时异常及可靠消息过期诊断使用中英文资源；已删除不再引用的就绪等待、客户端 Storage 和稳定身份资源键，默认与简体中文资源均为 39 项。

S09 验收结果：Core Messaging 聚焦测试在 net8.0、net9.0、net10.0 各 15/15；启用 `ZONGSOFT_MESSAGING_TESTS=true` 后 ZeroMQ 完整真实 Socket 套件三个目标各 65/65；MQTT 三目标各 18/18。Core、ZeroMQ 三目标构建成功，ZeroMQ 为 0 警告；Core 的 4 个 Security 警告为既有弃用／未使用警告。RabbitMQ、Kafka、Redis Streams、阿里云 MNS 和 MQTT 的全部目标框架构建均为 0 警告。CRLF、手写 C# Tab、资源键、Markdown 本地链接、旧运行符号和 `git diff --check` 均通过。

hosting 仍只有已部署的旧 NuGet 插件且本轮没有复制／部署授权，因此未用旧包冒充本地改动完成宿主冒烟；该限制不影响真实 Socket 集成测试结论，待本地插件获准部署后再运行 terminal/daemon 加载与关闭验证。

## 14. 存储契约、Control 异步化与端口命名清理

本阶段以第 13 节已确认的 `MostOnce`/`LeastOnce` 契约为基线。当前 net10.0 ZeroMQ 完整套件为 65/65；工作树包含用户已调整的 `Zongsoft.Messaging.ZeroMQ.option` 默认端口，本阶段保留该改动并在其上增量实施。

- [x] C01 登记本轮决策、当前 65/65 ZeroMQ 基线和用户已有工作树修改。
- [x] C02 重构 `IMessageStorage` 与 `MessageStorageBase<TSettings>`，删除逻辑分区参数。
- [x] C03 删除 `ResolveStorage`、MQTT 客户端 Storage 链路及相关失效资源和测试。
- [x] C04 清理 Packetizer 旧方法及本轮新增代码中的无引用符号。
- [x] C05 将 `ZeroPersistenceExecutor` 合并为非阻塞的 `ZeroControlServer.StorageWorker`。
- [x] C06 重写可靠接纳、ACK、过期、失败和关闭期间的存储完成状态机。
- [x] C07 将端口顺序改为 `Control,Incoming,Outgoing` 并统一 `Incoming/Outgoing` 术语。
- [x] C08 更新中英文 README、协议、技能文档、重构报告、本地化资源和测试记录。
- [x] C09 完成三目标构建、完整测试、格式及旧符号扫描，登记剩余后续事项。

第 12 节中的发布端存储、Terminal 分区、全部目标确认和 Ingress/Egress 术语均是已废止的历史设计，仅作决策演进记录；本节与当前协议文档表示最终运行契约。

C02～C04 已由 Core `MessageStorageBaseTest` 4/4、MQTT Server Storage 1/1 及三个 net10.0 测试项目构建验证。`IMessageStorage` 以 `Name` 表示实现名，以 `Settings` 界定独立实例的连接和数据作用域；所有存储方法已删除逻辑分区参数。MQTT 客户端空 Storage 链路已删除，仅保留 Server 的后续扩展点；`ResolveStorage`、Packetizer 旧解析链和相关失效资源无运行残留。

C07 已由 `ServerOptionsTests` 11/11 和 ZeroMQ Reliability/Server/Subscription/ServerOptions 聚焦套件 39/39 验证。三段配置与发现响应统一为 `Control,Incoming,Outgoing`，两段配置保持 `Incoming,Outgoing` 并在有 Storage 时随机绑定 Control。

C05/C06 已由四个新增真实 Socket 测试逐项通过并组合重复两轮，`ZeroQueueReliabilityTests` 为 19/19。阻塞 `SetAsync` 期间发现与 MostOnce Broadcast 仍能工作；ACK 后首次删除失败会停止当前进程重投并后台重试；Stop 会等待已接纳存储工作排空，重启后恢复持久消息；原生 DEALER 连续发送 1026 个 `PUBLISH` 可稳定触发 `StorageBusy`，被拒绝标识不会落盘。

C08 已同步中英文 README、2.0 协议、仓库技能文档和本报告。Core、MQTT、ZeroMQ 默认／简体中文资源键分别为 134/134、17/17、37/37；README 中英文各 8 个二级章节，本地 Markdown 链接均可解析。本地化文本未单独新增单元测试。

C09 最终验收结果：Core、ZeroMQ、MQTT 源项目在 net8.0、net9.0、net10.0 均构建成功；ZeroMQ 与 MQTT 为 0 警告，Core 仅保留 4 个与本次无关的既有 Security 警告。Core Messaging 聚焦测试三个目标各 17/17；启用 `ZONGSOFT_MESSAGING_TESTS=true` 后，ZeroMQ 完整真实 Socket 套件三个目标各 80/80，MQTT 完整套件三个目标各 17/17。RabbitMQ、Kafka、Redis Streams、阿里云 MNS 的全部目标框架构建均为 0 警告、0 错误。

最终旧符号扫描确认运行时代码中不存在 `ResolveStorage`、`ZeroPersistenceExecutor`、`GetStoreName`、逻辑分区参数、旧 Storage 类型、Reliability 后缀方法、`Ingress/Egress` 或 Terminal 协议状态，也不存在 Poller 对 Storage I/O 的同步等待。hosting 仍只部署旧 NuGet 插件，按“不复制、不部署”约束未用旧包冒充本地成果；本地插件获准部署后的 terminal/daemon 加载与关闭冒烟，以及 Redis/SQLite 等独立 Storage 插件，继续作为后续里程碑。

变更前 net10.0 ZeroMQ 完整套件为 68/68；当前工作树包含上一阶段尚未提交的 Core、ZeroMQ、MQTT 与文档成果，本阶段保留并在其上增量实施。

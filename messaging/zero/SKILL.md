---
name: zongsoft-zeromq
description: 创建、审查、调试、测试或重构 messaging/zero 下的 Zongsoft.Messaging.ZeroMQ NetMQ 适配器，包括队列生命周期、广播路由可见性、可靠 Control 投递、线协议、请求响应、事件传输和插件宿主；不用于无关的 ZeroMQ 应用。
---

# Zongsoft ZeroMQ 适配器

本技能用于 `messaging/zero`。保持 `Zongsoft.Core/src/Messaging` 和 `Zongsoft.Core/src/Communication` 建立的契约，不要把本项目当作孤立的 NetMQ 包装器。

## 从当前状态开始

1. 阅读 [AGENTS.md](AGENTS.md)、[../AGENTS.md](../AGENTS.md)；诊断、规划或重构时阅读 [REFACTORING.zh-Hans.md](REFACTORING.zh-Hans.md)，使用其中结论前以当前源码重新核对。
2. 编辑前检查 `git status --short`，不要覆盖或还原用户拥有的项目及部署文件变更。
3. 只阅读任务相关的 Core 抽象：
	- 队列：`MessageQueueBase`、`MessageConsumerBase`、`IMessageQueue`、`Message`、`MessageOptions`、`MessageReliability`。
	- 请求响应：`IRequester`、`IResponder`、`IRequest`、`IResponse`、`IRequestToken`。
	- 事件：`IEventChannel`、`EventExchanger`、`EventContext`。
4. 提议变更前检查对应 ZeroMQ 实现和测试。含 sleep 或重复发送的测试只能作为集成证据，不能证明订阅 Ready 语义。

## 架构

- `ZeroQueueServer` 暴露 Discovery REP、MostOnce XSUB/XPUB 数据端点和可选 LeastOnce ROUTER Control 端点。`ServerAgent` 拥有单一 Poller；`ZeroBroadcastServer` 拥有 XSUB/XPUB；`ZeroControlServer` 拥有在线注册、Broker Pending、竞争投递、重试、ACK 和有界 `StorageWorker`。存储结果通过 Actor 命令返回 Poller。
- `ZeroQueue` 是唯一公共队列门面。私有 `ZeroQueue.Transport` Actor 负责 Discovery；内部 `ZeroBroadcast` 和 `ZeroControl` 共享 Poller，分别拥有 XPUB/SUB 广播状态与 DEALER 可靠控制状态。没有第二个生产传输前不要引入 Transport 接口。
- 一个 `ZeroSubscriber` 使用一个物理 Topic 的 `SubscriberSocket` 和有界、有序 Handler Channel。`MessageQueueBase.Subscribers` 是唯一逻辑 Topic 注册表：初始化项可共享但不可见，只有成功初始化的消费者进入公共活动视图。
- `ZeroRequester`/`ZeroResponder` 将通信 URL 映射为队列 Topic；`ZeroQueueEventChannel` 将事件映射到 `Events/...`。
- daemon 插件启动 `ZeroQueueServer`；主插件注册连接驱动、事件传输、Requester 和 Responder。

## 不可破坏的不变量

- 每个 `NetMQSocket` 的创建相关操作、收发和确定性释放都限制在其所属 Poller/Actor 线程。跨线程调用通过 `NetMQQueue`、Channel 或 Actor 边界提交。
- Server Poller 绝不能同步等待 `IMessageStorage`。存储工作进入有界队列，完成后先通过 `ServerAgent` 回到 Poller，再改变 Control 状态或访问 ROUTER Socket。
- 不用固定延迟、`HasOut` 或本地发送成功证明远端订阅已传播。区分传输连接、订阅传播、本地接受、实际发送和 Handler 完成。
- 保持不同契约：`MostOnce` 在应用 XPUB 当下没有匹配订阅时不发送并返回 `null`，否则在一次本地发送后完成；`LeastOnce` 在 Broker 没有在线匹配时返回 `null`，否则在 Broker Pending 持久化后完成。两者都不是 `ExactlyOnce`。
- `ProduceAsync` 在延迟工作前快照 Payload，并为每次发布生成唯一标识。非空标识不代表远端或 Handler 确认；ZeroMQ Publisher 永不持久化消息。
- `ProduceAsync` 完成语义必须与 Payload 所有权一起定义；若延迟发送，返回前必须快照借用内存，或明确并强制等价生命周期。
- 新 Subscriber 初始化失败或取消时必须回滚并释放。初始化和活动状态都保留在 `MessageQueueBase.Subscribers`，不增加第二个 Topic 字典，也不公开失败/初始化中的项。
- Queue 关闭或释放时关闭消费者、解绑 Handler、停止异步工作并释放 Socket，不能与 Poller 竞态。
- 区分逻辑 Topic 和带 Group 的物理 Topic。Group 只添加一次，并在事件或请求响应路由前规范化。
- 每 Subscriber 分派保持有界和有序。容量压力只暂停该 Subscriber Socket；Handler 释放容量后通过 Actor 命令恢复。
- Handler 并发必须有界并定义顺序/背压；不得为每条消息创建无界 `Task.Run`。
- 防御性解析不可信 Frame：校验帧数、Header 分隔符、Option、Compression、Payload 大小和空载荷语义，不能让错误终止 Poller。
- 可选能力通过 `IMessageQueue.Features` 声明。Core 在队列未声明 `MessageQueueFeature.Delay` 时拒绝正 `Delay`，ZeroMQ 不声明 Delay。Tags、Expiration、Priority、Reliability、Compression 和回退行为必须实现或明确记录。
- 传输无关的 Option 规范化、可靠性能力校验和重复订阅一致性放在 Core `MessageQueueBase`；Delivery Tag、Offset Commit、ACK 路由、重试窗口、持久化和 Socket 状态留在驱动。抽取前比较 RabbitMQ、Kafka、MQTT、Redis、Aliyun，确认语义真正相同。
- 保留 `Message.AcknowledgeAsync` 显式确认；除非 Core 对所有驱动统一改变契约，否则 Handler 正常返回不是隐式确认。

## 当前 1.0 线格式

- Discovery 是带版本的文本请求响应，包含 `Epoch` 和一个 `Ports` Header。启用 Control 时端口为 `Control,Incoming,Outgoing`，否则为 `Incoming,Outgoing`。
- 数据消息有两帧：
	1. UTF-8 Header：`<effective-topic>`，随后是 `Protocol-Version:1.0`、`Identifier:<id>`、`Identity:<instance>`，以及可选 `Tags:<tags>`、`Compression:<algorithm>`。
	2. 二进制 Payload。
- `MessageEnqueueOptions.Compression` 是 `MessageCompression`：`Name` 为算法，整数 `Value` 为字节阈值；文本格式为 `<algorithm>:<threshold>`。值类型不定义传输信封，无法单独携带元数据的驱动自行拥有私有 Payload Framing。支持 Brotli、GZip、ZLib、Deflate，只压缩业务 `Message.Data`。
- 每个业务 Header 包含 `Protocol-Version:1.0`；XPUB Welcome Frame 包含与 Discovery 相同的 Broker Epoch。
- Heartbeat 是匿名空 Payload 消息，不得与合法的空业务消息混淆。
- Request Payload 前缀为 `<request-identifier>\n`；Response 使用相同标识前缀，通常发布到 `<url>/reply`。

LeastOnce 使用 ROUTER/DEALER 命令 `REGISTER`、`UNREGISTER`、`PING`、`PUBLISH`、`DELIVER`、`ACK`、`ACCEPTED`、`UNROUTABLE`、`ERROR`。`PUBLISH` 和 `DELIVER` 携带 Identifier、物理 Topic、Producer Identity、Tags、原始 Timestamp、Expiration/Attempt、Compression 和 Payload。Broker 校验但不解压业务 Payload。Session/Subscription 标识是运行时路由身份，不是持久业务身份。

## 可靠投递实现

- 规范线协议见 [PROTOCOL.md](PROTOCOL.md) 和 [PROTOCOL.zh-Hans.md](PROTOCOL.zh-Hans.md)；若 `.testagent/status.md` 存在，可用于了解实现状态，但需以源码复核。
- Stage 2A 的 `MessageReliability.MostOnce` 依赖应用 XPUB 的即时订阅检测，只在该 XPUB 当前知道匹配 Prefix 时发送，没有确认和重试。
- 订阅传播只代表路由可见性；不等待未来订阅，也不据此推断远端收到消息。
- Discovery 和 Welcome 携带 Broker Epoch；断连或 Epoch 变化会使缓存订阅 Ready 失效，并重新发现固定或随机运行时端口。
- MostOnce 没有匹配 Prefix 时立即返回 `null`，以后也不会补发。
- Stage 2B 的 `LeastOnce` 使用可寻址运行时 Session、显式 `Message.AcknowledgeAsync`、仅 Broker 持久化、竞争消费者和同标识重试；重复投递是契约的一部分。
- Broker 接受要求存在在线匹配订阅，先持久化 Pending 再返回 `ACCEPTED`，不等待 Handler ACK。每次尝试选择一个在线消费者，任意有效 ACK 删除 Pending；新订阅或重连订阅可消费已接受 Pending。
- Core 拥有传输无关的 `IMessageStorage` 与 `MessageStorageBase<TSettings>`。Storage 是独立插件，每个 Broker 使用独立配置实例。基类用一个模板方法处理全部消息和精确 Topic 的读/清理：null Topic 表示不过滤，空字符串表示默认 Topic。Server 仅在 `Disposable=true` 时释放 Storage，优先 `IAsyncDisposable`；Stop 不释放。持久化完整外层 `Message` 元数据，ZeroMQ 的 Expiration、Protocol Version、Compression、Retry 信封保持私有。
- 不支持 `ExactlyOnce`，并且必须在创建传输状态前失败。

## 配置事实

- Client 配置位于 `/Messaging/ConnectionSettings`，Driver 为 `ZeroMQ`。
- `Server` 必填；Discovery `Port` 默认 `7969`；`Timeout`、`Heartbeat` 默认 `10s`。
- `Topic`、`Group`、`Client`、`Instance`、`Filter` 影响路由或身份。默认 Filter 排除当前 Instance；`Filter=*` 接收所有实例。
- `ReconnectInterval` 控制重新发现。
- Server 端口位于 `/Messaging/ZeroMQ/Servers`。三个值表示 `Control,Incoming,Outgoing`；两个值表示 `Incoming,Outgoing`，挂载 Storage 时随机绑定 Control。优先级为启动参数、命名 Server 自身 Port、集合默认值，仍未定义则随机。
- Server Storage 只能在停止状态变更。没有 Storage 的 Broker 从 Discovery `Ports` 省略 Control，但继续提供 Broadcast。`IMessageStorage.Name` 标识 Provider，`Settings` 定义独立实例的连接与数据范围，并支持精确 Topic 读/清理。
- Server TCP 端点绑定所有接口，本适配器不配置认证或加密。

## 验证

对每个受影响目标框架先构建 Core，再构建本适配器：

```powershell
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net8.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net9.0
dotnet build Zongsoft.Core\src\Zongsoft.Core.csproj -f net10.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net8.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net9.0
dotnet build messaging\zero\src\Zongsoft.Messaging.ZeroMQ.csproj -f net10.0
```

集成测试显式启用，并使用真实 Socket/Poller：

```powershell
$env:ZONGSOFT_MESSAGING_TESTS = 'true'
dotnet test messaging\zero\test\Zongsoft.Messaging.ZeroMQ.Tests.csproj -f net10.0 --no-restore --blame-hang-timeout 2m
```

- NetMQ 测试共享进程级状态和网络资源，各目标框架串行运行。
- 首条发布回归先运行整套测试，再单独重复 `ZeroQueueConcurrencyTests.ConcurrentProduceOnSingleQueueIsThreadSafe`；单独通过不能排除顺序相关竞态。
- 相关区域变化时增加空 Payload、订阅初始化失败/取消回滚、带 Group 的 Event/RPC Topic、活动消费者下释放 Queue、畸形 Frame 和 Broker 重启测试。
- 独立 samples 用于冒烟。只有插件已刻意部署到隔离宿主时才使用 `D:\Zongsoft\hosting` terminal/daemon；未经授权不运行部署/安装脚本或修改宿主包。

## 文档与样式

- `README.md` 与 `README.zh-Hans.md` 面向使用者并保持同步；实现分析放在 `REFACTORING.zh-Hans.md`。
- 面向用户的异常和诊断文本放入 `src/Properties/Resources.resx`，并添加对应 `Resources.zh-Hans.resx`；协议 Token、Topic、Endpoint Scheme 和线字段名保持文化无关。
- 仓库文本使用 CRLF，代码和代码示例使用 Tab；Unix 脚本按要求使用 LF。
- 运行时行为变化时，同步 README 能力矩阵、投递语义、测试和实现评估状态。

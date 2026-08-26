# Zongsoft.Messaging.ZeroMQ 架构与重构评估

## 1. 当前架构

`ZeroQueue` 是唯一公共消息队列门面，继承 Core `MessageQueueBase`，由 Core 统一处理可靠性上限、功能校验、并发订阅事务和唯一的 `Subscribers` 注册表。

| 语义 | 客户端组件 | 服务端组件 | Socket | 完成点 |
| --- | --- | --- | --- | --- |
| `MostOnce` | `ZeroBroadcast` | `ZeroBroadcastServer` | XPUB/SUB 与 XSUB/XPUB | 匹配订阅可见时完成一次本地发送 |
| `LeastOnce` | `ZeroControl` | `ZeroControlServer` | DEALER/ROUTER | Broker 持久化 Pending 后接纳 |
| 请求响应 | `ZeroRequester` / `ZeroResponder` | 无专用 Broker 组件 | 当前仍适配队列主题 | 由通信适配器定义 |

客户端 `Transport` Actor 拥有发现状态机和全部客户端 NetMQ Socket；Broadcast 与 Control 共享其 Poller。服务端 `ServerAgent` 拥有 REP、XSUB、XPUB、ROUTER 及一个 Poller。跨线程调用只投递命令，不直接访问 Socket。

## 2. 投递语义

### 2.1 MostOnce

- 发布瞬间应用 XPUB 没有匹配订阅时立即返回 `null`，不等待未来订阅者，也不发送消息。
- 存在匹配订阅时发送一次并返回唯一消息标识；非空标识不证明远端入队或 Handler 已执行。
- 多个匹配订阅者收到相同的 Identifier、Identity、Tags 和业务负载。
- Broadcast 是瞬态传输，没有持久化、确认或重试。

### 2.2 LeastOnce

- 没有在线匹配订阅时返回 `null`，Broker 不写 Storage。
- 有匹配订阅时，Broker 先将 Pending 写入 `IMessageStorage`，成功后立即向发布者返回标识，不等待消费者确认。
- Broker 在在线订阅者间竞争投递。未确认时以同一 Identifier 重投，也可能更换消费者；任一有效 ACK 都会停止投递并异步删除 Pending。
- ACK 与持久删除之间发生故障时可能再次投递，消费者必须按 Identifier 保证业务幂等。
- 超时、取消或断线可能使发布者无法确定 Broker 是否已经接纳；业务重试必须容忍重复。

ZeroMQ 的可靠性上限为 `LeastOnce`。`ExactlyOnce` 是否受支持由其他消息队列提供程序自行决定，不是 Core 全局限制。

## 3. 消息存储

`IMessageStorage` 是独立于任何消息驱动的 Core 契约：

- `Name` 表示实现名称，`Settings` 定义独立实例的连接和数据作用域；
- `SetAsync` 完成前必须持有 Message 及其 Data、Tags 等可变内容的快照；
- `GetAsync()` 用于 Broker 启动恢复，`GetAsync(topic)` 使用区分大小写的精确主题匹配；`MessageStorageBase<TSettings>` 将两者统一委托给一个模板方法，以 `null` 表示不限定主题、空字符串表示默认主题；
- `RemoveAsync` 按 Identifier 删除；两个 `ClearAsync` 分别清除全部或精确主题的消息并返回实际删除数，基类同样以 `null` 哨兵统一委托给一个模板方法；
- 返回的恢复消息不包含确认回调；Storage 实现负责 TTL 过滤。

每个队列服务器挂载独立 Storage 实例。普通 Stop 不释放 Storage；Server 自身 Dispose 时，仅当 `Storage.Disposable=true` 才释放它，并优先使用 `IAsyncDisposable`。ZeroMQ Broker 未挂载 Storage 时仍提供 Broadcast，但不启动 Control。

`ZeroControlServer.StorageWorker` 使用容量 1024 的单读者通道串行执行存储 I/O。Poller 只做非阻塞投递，完成结果通过 `ServerAgent` 命令队列返回 Poller 后才修改协议状态或发送 ROUTER 响应。

## 4. 压缩与业务元数据

`MessageCompression` 使用 `Name` 表示算法、使用整数 `Value` 表示字节阈值；解析和格式化文本为 `<algorithm>:<decimal-byte-threshold>`。它不定义任何传输信封，无法独立携带压缩元数据的驱动应在各自协议层维护私有负载封装。默认值不压缩，阈值零压缩全部非空 Data，负数非法。当前支持 Brotli、GZip、ZLib 和 Deflate。

压缩只作用于 `Message.Data`：

- Broadcast 首帧独立携带 Identifier、Identity、Tags 和可选 Compression，第二帧为业务负载；
- Control 的 PUBLISH 和 DELIVER 使用固定 Compression 帧，未压缩时为空；
- Broker 不解压可靠消息，只验证算法名、持久化并重投压缩负载；订阅端解压后才构造 Message；
- 心跳、发现、注册、ACK 等控制数据不压缩。

Core 通过 `MessageQueueFeature.Compression` 表达能力，并在进入不支持的驱动前抛出 `OperationException.Unsupported`。

## 5. 协议与端口

当前协议版本固定为 `1.0`，不兼容旧帧或旧 Pending 数据。协议名称、字段、命令、错误码和大小限制统一定义在内部 `Protocol` 类中；`Packetizer` 只负责 Broadcast 首帧编解码。

发现成功响应只包含 `Protocol-Version`、`Epoch` 和 `Ports`。启用 Control 时端口顺序是 `Control,Incoming,Outgoing`；未启用时是 `Incoming,Outgoing`。详细格式参见 [中文协议](PROTOCOL.zh-Hans.md)或[英文协议](PROTOCOL.md)。

服务端端口解析优先级：

1. 启动参数显式指定；
2. 命名服务器自身的 `Port`；
3. 服务器集合的默认 `Servers.Port`；
4. 随机端口。

固定运行端口仍有利于防火墙配置，但 Broker 重启后客户端会根据新的 Epoch 和发现结果重新连接。

## 6. 背压与生命周期

- 每个 `ZeroSubscriber` 使用容量 1000 的有界顺序通道；满载时只暂停对应 SubscriberSocket，消费腾出空间后由 Actor 恢复。
- Handler 异常会记录并隔离，不会终止 Poller 或后续处理。
- Queue 关闭时先关闭活动订阅和处理循环，再关闭 Actor；Socket 在所属 Poller 线程确定性释放。
- 初始化失败、取消或关闭会回滚 Core 订阅项；竞争失败创建的消费者会立即释放。
- Queue 构造时快照连接、端口、分组、过滤、超时、心跳和重连设置，不反向修改配置对象。

## 7. 当前限制与后续事项

| 事项 | 影响与建议 |
| --- | --- |
| Broadcast 瞬态 | 订阅不可见、断线或发送后故障都可能丢失，只用于允许丢失的通知。 |
| LeastOnce 重复 | ACK 丢失或删除前故障会重投，消费者必须业务幂等。 |
| 接纳结果不确定 | Control 超时或断线时由业务决定是否重试，并容忍重复。 |
| Storage 性能 | 每条可靠消息需要持久写入；实现应公开耐久级别、容量、延迟和监控指标。 |
| 网络安全 | TCP 端点默认绑定外部接口且无认证、加密，应部署在可信边界或安全隧道内。 |
| 请求响应适配 | `ZeroRequester`/`ZeroResponder` 仍使用队列主题，后续可独立评估专用 DEALER/ROUTER 通道。 |
| Storage 插件 | Redis、SQLite、DuckDB、etcd 应作为独立插件实现；SQLite/Redis 优先，DuckDB/etcd 需明确适用限制。 |

当前实施进度和测试证据仅记录在 [`.testagent/status.md`](../../.testagent/status.md)，避免把阶段过程混入架构说明。

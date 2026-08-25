# Zongsoft.Messaging.ZeroMQ 重构评估

## 1. 当前结论

ZeroMQ 驱动已经形成三条边界清晰的通道：

| 语义 | 组件 | 传输与完成点 |
| --- | --- | --- |
| 瞬态广播 | `ZeroBroadcast` / `ZeroBroadcastServer` | XPUB/XSUB/SUB；本地发送后完成 |
| 至少一次 | `ZeroControl` / `ZeroControlServer` | DEALER/ROUTER；Broker 持久接纳后完成 |
| 请求应答 | `ZeroRequester` / `ZeroResponder` | 基于独立请求响应主题的通信适配器 |

`ZeroQueue` 是唯一公共队列门面，通用可靠性和功能校验由 Core `MessageQueueBase` 负责。客户端 Actor 与服务端 Poller 分别拥有其全部 NetMQ Socket；调用线程只投递命令。可靠通道的 Storage I/O 通过有界 `StorageWorker` 串行执行，结果回到 Poller 后才修改协议状态。

## 2. 当前投递契约

### 2.1 MostOnce

- 发布瞬间没有匹配的可见订阅时返回 `null`，消息不会发送。
- 存在匹配订阅时通过应用 XPUB 本地发送一次并返回唯一标识。
- 非空标识不代表远端收到，也不代表 Handler 已执行。
- 多个匹配订阅者都会收到同一标识。
- Broadcast 使用双帧业务协议，支持空载荷和可选 Brotli 压缩。

### 2.2 LeastOnce

- Broker 只在存在在线匹配订阅时接纳发布，否则返回 `null`。
- Broker 在返回 `ACCEPTED` 前把 Pending 消息写入 `IMessageStorage`。
- 接纳完成后在在线订阅者间竞争投递；未确认时沿用相同标识重投。
- 任一有效 `AcknowledgeAsync` 会停止投递，并异步删除持久消息。
- 消费者离线后，已经接纳的消息继续保留，订阅恢复后重新投递。
- 重复投递属于契约的一部分，业务处理器必须按 `Message.Identifier` 幂等。

ZeroMQ 的可靠性上限为 `LeastOnce`。`ExactlyOnce` 是否可用由具体消息队列提供程序决定，不是 Core 的全局限制。

## 3. Core 契约

### 3.1 消息存储

`IMessageStorage` 是独立于消息队列驱动的公共契约：

- `Name` 表示存储实现名称；
- `Settings` 定义单个实例的连接与数据作用域；
- `SetAsync` 在完成前必须持有完整消息快照；
- `RemoveAsync` 按 `Message.Identifier` 删除消息；
- `GetAsync` 用于 Broker 启动恢复。

每个 `ZeroQueueServer` 使用独立 Storage 实例。实例生命周期由插件容器或应用负责，Server 不释放外部 Storage。未配置 Storage 时，Server 仍提供 Broadcast，发现响应中的 `Control` 为零。

### 3.2 功能集

`IMessageQueue.Features` 表达驱动实际支持的可选能力。`MessageQueueBase` 在进入具体驱动前统一验证功能请求：

- `MessageQueueFeature.Delay` 表示延迟入队；
- 当前 ZeroMQ 不声明 Delay，因此正数 `MessageEnqueueOptions.Delay` 会抛出 `OperationException.Unsupported`；
- `MessageEnqueueOptions.Compression` 表示启用压缩的最小载荷字节数，非正数关闭压缩；ZeroMQ 当前只在 `MostOnce` Broadcast 路径使用该阈值。

## 4. 线程、背压与生命周期

- `ZeroQueue.Transport` 拥有客户端 Actor、发现状态机和 Poller，`ZeroBroadcast` 与 `ZeroControl` 共享它。
- `ZeroQueueServer.ServerAgent` 拥有服务端 Poller，`ZeroBroadcastServer` 与 `ZeroControlServer` 共享它。
- 每个 `ZeroSubscriber` 使用容量 1000 的有界顺序通道。满载时只暂停对应 SubscriberSocket，消费腾出空间后通过 Actor 命令恢复。
- Handler 异常被记录并隔离，不会终止 Poller 或后续消息处理。
- Queue 关闭时先关闭活动订阅与处理循环，再关闭 Actor；Socket 始终在所属线程释放。
- `MessageQueueBase.Subscribers` 是唯一逻辑订阅注册表，并发首次订阅共享同一个初始化任务；失败、取消和关闭都会回滚缓存。

## 5. 协议与主题

- 发现响应包含 `Epoch`、`Control`、`Incoming` 和 `Outgoing`。
- 服务端端口顺序为 `Control,Incoming,Outgoing`，默认 `32100,32101,32102`。
- 业务主题在 API 层保持逻辑值，传输边界精确添加或移除一次 `Group:` 前缀。
- Broadcast 业务帧包含协议版本、消息标识、生产者实例和可选压缩器。
- Control 命令覆盖订阅登记、发布接纳、竞争投递和显式确认。
- 所有外部帧都会校验帧数、头格式、标识、时间、选项和大小；畸形消息只丢弃当前消息并记录本地化诊断。

完整帧定义参见 [2.0 协议](PROTOCOL-2.0.zh-Hans.md)。

## 6. 当前限制与后续建议

| 事项 | 当前影响 | 建议 |
| --- | --- | --- |
| Broadcast 是瞬态传输 | 订阅不可见、断线或发送后进程故障都可能丢失消息 | 只用于允许丢失的通知；需要耐久性时选择 `LeastOnce` |
| LeastOnce 允许重复 | ACK 响应丢失或 Broker 在删除完成前故障会再次投递 | 消费者以消息标识实现业务幂等 |
| Control 接纳结果可能不确定 | 超时、取消或断线时发布者无法确认 Broker 是否已经接纳 | 业务重试必须容忍重复 |
| 无认证和加密 | TCP 端点默认暴露于外部接口 | 部署在可信网络边界，通过防火墙或安全隧道保护 |
| 每条可靠消息都需要持久写入 | Storage 延迟直接影响发布接纳延迟 | Storage 插件应提供明确耐久级别、容量限制和监控指标 |
| 请求应答仍是队列主题适配 | 生命周期和错误语义受队列传输影响 | 后续可独立评估专用 DEALER/ROUTER 请求应答协议 |

独立 Storage 插件建议优先实现 SQLite 和 Redis；DuckDB 更适合批量归档，etcd 更适合小型控制状态。具体插件应只依赖 Core `IMessageStorage`，不得引用 ZeroMQ。

## 7. 当前实施清单

- [x] F01 调研 Data Feature 范式、Core 队列契约和各驱动能力。
- [x] F02 收敛 ZeroMQ 文档，仅保留当前功能描述。
- [x] F03 为 `MessageEnqueueOptions` 增加压缩属性并迁移 ZeroMQ。
- [x] F04 新增 `MessageQueueFeature` 与集合并接入 `IMessageQueue`／基类。
- [x] F05 各驱动声明功能并统一校验不支持的 Delay。
- [x] F06 增补聚焦测试并核对需求映射。
- [x] F07 完成跨目标构建、完整测试和格式／符号检查。

## 8. 验收方式

- Core Messaging 聚焦测试覆盖 Feature 值对象、集合、接口暴露、Delay 支持与拒绝、压缩阈值和订阅事务。
- ZeroMQ 真实 Socket 套件覆盖广播压缩、发布、订阅、可靠接纳、ACK、重投、Broker 恢复、背压、协议健壮性和生命周期。
- Core、ZeroMQ、MQTT 在 net8.0、net9.0、net10.0 构建和测试。
- Kafka、RabbitMQ、Redis、阿里云完成全部目标框架构建；可运行的聚焦测试不依赖外部 Broker。
- 检查默认／简体中文资源键、Markdown 链接、CRLF、C# Tab 和 `git diff --check`。

当前验收结果：Core Messaging 在 net8.0、net9.0、net10.0 各 22/22；启用 `ZONGSOFT_MESSAGING_TESTS=true` 后，ZeroMQ 完整真实 Socket 套件各 82/82，MQTT 完整套件各 17/17。ZeroMQ、MQTT、RabbitMQ、Kafka、Redis 和阿里云全部目标框架构建通过；Core 仅保留 4 个既有 Security 警告。Core、ZeroMQ、MQTT 默认与简体中文资源键分别为 135/135、37/37、17/17，中英文 README 各 8 个二级章节，本地链接、CRLF、C# Tab 和差异检查均通过。

hosting 当前加载的是已部署包而不是本地工作树构建。本地插件完成隔离部署后，再使用 terminal/daemon 验证插件加载与关闭；本报告不把其他版本的宿主结果作为当前代码证据。

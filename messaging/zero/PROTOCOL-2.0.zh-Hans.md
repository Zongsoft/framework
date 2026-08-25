# Zongsoft.Messaging.ZeroMQ 2.0 投递协议

本文说明当前 ZeroMQ 驱动的协议、组件边界与投递契约。

## 1. 语义边界

| 模式 | 无匹配订阅 | `ProduceAsync` 成功完成点 | 后续保障 |
| --- | --- | --- | --- |
| `MostOnce` | 返回 `null`，不发送 | 应用 XPUB 完成本地发送，返回唯一标识 | 无持久化、ACK 或重试 |
| `LeastOnce` | Broker 返回 `UNROUTABLE`，客户端返回 `null` | Broker 把 Pending 写入 Storage 后返回 `ACCEPTED` | 竞争投递，同标识重投，任一 ACK 删除 Pending |
| `ExactlyOnce` | 不支持 | — | — |

`null` 只表示发送瞬间没有可见的匹配订阅。非空标识不表示远端 Socket 已入队，也不表示 Handler 已执行。`LeastOnce` 的保证从 Broker 持久接纳开始；Control 超时、调用取消或断线可能使发布者无法确定 Broker 是否已经接纳。这些操作只停止客户端等待，不能撤销 Broker 已开始的持久化，业务重试可能产生重复。

## 2. 组件与线程归属

- `ZeroQueue` 是唯一公共队列门面，`MessageQueueBase.Subscribers` 是唯一逻辑订阅注册表。
- `ZeroQueue.Transport` 拥有客户端 Actor、发现状态机和 Poller；内部 `ZeroBroadcast` 与 `ZeroControl` 共享该 Poller。
- `ZeroQueueServer.ServerAgent` 拥有服务端 Poller；内部 `ZeroBroadcastServer` 与 `ZeroControlServer` 共享该 Poller。
- `ZeroBroadcast`／`ZeroBroadcastServer` 只处理 `MostOnce` 的 XPUB/XSUB/SUB Socket。
- `ZeroControl`／`ZeroControlServer` 只处理 `LeastOnce` 的 DEALER/ROUTER Socket、在线登记、Pending、竞争投递和 ACK。
- `ZeroRequester`／`ZeroResponder` 是请求应答适配器，不属于 Control 可靠投递协议。

所有 NetMQ Socket 的创建、使用和释放均归属其 Poller 线程。调用线程只提交命令，不直接访问 Socket；内部组件不创建额外 Actor 线程。

## 3. 发现协议

客户端向发现 REP 发送：

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:2.0
```

成功响应：

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:2.0
Epoch:<broker-epoch>
Control:<reliable-control-port>
Incoming:<publisher-incoming-port>
Outgoing:<subscriber-outgoing-port>
```

`Epoch` 在每次 Server 启动时变化。端口是十进制无符号整数。Broker 未配置 `IMessageStorage` 时不启动可靠 Control，并返回 `Control:0`。客户端在断线或 Epoch 变化后清除当前端点与 XPUB 订阅传播集合，重新发现并重建 Socket；固定和随机运行端口均支持重新发现。

## 4. Broadcast（MostOnce）

### 4.1 拓扑

```text
Application XPUB → Broker XSUB → Broker XPUB → Application SUB
```

应用 XPUB 接收 Broker 传播的标准订阅控制帧，并维护当前 Epoch 下的物理主题前缀集合：首字节 `0x01` 表示订阅，`0x00` 表示取消，其余字节为 UTF-8 前缀。物理主题 `T` 在存在前缀 `P` 满足 `T.StartsWith(P, Ordinal)` 时视为当前可投递；空前缀匹配所有主题。

该集合只是发送瞬间的路由可见性，不是远端收件确认。取消订阅、断线或 Epoch 变化会立即影响后续发布；没有匹配前缀的发布即时返回 `null`。

### 4.2 业务帧

Broadcast 业务消息固定为两帧：

```text
Frame 0 (UTF-8): <physical-topic>@<producer-instance>\nProtocol-Version:2.0\nIdentifier:<message-identifier>[\nCompressor:Brotli]
Frame 1 (binary): payload
```

- `Identifier` 为每次发布生成的唯一标识，必须非空；所有广播接收者得到相同的 `Message.Identifier`。
- `physical-topic` 是逻辑主题精确添加一次 `Group:` 前缀后的值；接收端移除该精确前缀，`Message.Topic` 始终是逻辑主题。
- `Compressor:Brotli` 表示第二帧需要解压；未知压缩器或损坏载荷只丢弃当前消息。
- 生产者通过 `MessageEnqueueOptions.Compression` 指定启用压缩的最小载荷字节数，非正数表示关闭。
- 空业务载荷合法。只有匿名实例且空载荷的内部帧才可视为心跳。
- 帧数、主题头、选项、标识、大小或 UTF-8 不合法时，只记录本地化诊断并丢弃当前消息，不得终止 Poller。

### 4.3 发布状态

1. API 边界复制调用方负载并生成标识。
2. Actor 收到发布命令时检查当前匹配前缀。
3. 无匹配前缀时立即以 `null` 完成，不调用 XPUB Socket。
4. 有匹配前缀时发送一次，以生成的标识完成。
5. 发送后的取消不能撤销消息；发送前取消或 Queue 关闭按标准取消／释放异常完成。

心跳只在已有匹配订阅时发送；无订阅时跳过，不产生业务发布结果。

## 5. Control（LeastOnce）

### 5.1 拓扑与命令

Control 使用独立 ROUTER／DEALER 端点，因为可靠订阅需要可寻址会话、Broker 接纳结果和显式 ACK，而 Broadcast 的 XPUB 聚合订阅帧不能提供这些语义。

DEALER 发送帧（ROUTER 接收时最前面另有路由身份帧）：

| 命令 | 后续帧 |
| --- | --- |
| `REGISTER` | Session、Subscription、PhysicalTopic |
| `UNREGISTER` | Session、Subscription |
| `PING` | Session、Subscription |
| `PUBLISH` | Identifier、PhysicalTopic、Producer、Tags、TimestampTicks、ExpirationTicks、Payload |
| `ACK` | Session、Subscription、Identifier |

ROUTER 返回或投递：

| 命令 | 后续帧 |
| --- | --- |
| `REGISTERED` | Subscription |
| `UNROUTABLE` | Identifier |
| `ACCEPTED` | Identifier |
| `DELIVER` | Subscription、Identifier、PhysicalTopic、Producer、Tags、TimestampTicks、Attempt、Payload |
| `ERROR` | ErrorCode、Identifier |

命令字、字段名和错误码是协议常量，不本地化。帧数、标识、时间、大小或状态不合法时，只拒绝当前命令；Storage 异常返回 `ERROR`，不得终止 Poller。

### 5.2 接纳与完成

Broker 收到 `PUBLISH` 后：

1. 验证帧并查询当前在线、主题匹配的可靠订阅。
2. 没有匹配订阅时返回 `UNROUTABLE`，不写 Storage，客户端返回 `null`。
3. 存在匹配订阅时，把完整外层 `Message` 快照写入 Pending 存储区。
4. `SetAsync` 成功后加入内存 Pending，返回 `ACCEPTED`，客户端立即以相同 Identifier 完成。
5. 首次投递与后续 Handler、ACK 状态不再反向改变已经完成的生产调用。

存储 I/O 由有界单读取器工作队列串行执行，完成结果通过 Actor 命令返回 Poller，Poller 不同步等待 Storage。工作队列已满时返回 `StorageBusy`，不建立 Pending。

如果相同 Identifier 已在接纳中或 Pending 且消息内容一致，Broker 共享或幂等返回 `ACCEPTED`；内容不一致则返回 `IdentifierConflict`。发布者不持有 Storage，Broker 是可靠消息持久状态的唯一所有者。

### 5.3 竞争投递与确认

- Broker 按物理主题维护轮询位置，每次从在线匹配订阅中选择一个消费者。
- 未收到 ACK 时递增 `Attempt`，沿用同一 Identifier 定时重投，并可改投其他在线消费者。
- Handler 必须主动调用 `Message.AcknowledgeAsync`；正常返回、异常或取消都不自动确认。
- 任一当前有效会话的 ACK 会立即标记待删除并停止当前进程内的重投；Storage 删除成功或记录已不存在后再清理内存 Pending。
- Storage 删除暂时失败时保留待删除状态并后台重试；ACK 与持久删除之间崩溃仍可能在重启后导致重复投递。
- 实例过滤命中排除规则时不调用 Handler，由驱动确认该次投递，避免 Pending 永久阻塞。
- 全部订阅者离线不会删除已接纳消息；订阅恢复后继续竞争投递。
- 新注册订阅可以消费已有 Pending。运行期 Session 和 Subscription 只用于路由与 ACK，不要求稳定的 `Client` 或 `Instance`。

重复投递是正常行为。消费者必须用 `Message.Identifier` 实现业务幂等。

### 5.4 存储与过期

只有 `ZeroQueueServer.Storage` 参与 ZeroMQ 可靠投递。Storage 的生命周期由注入它的插件或应用拥有，Server 不释放；运行中的 Server 不允许替换 Storage。

`IMessageStorage.Name` 表示 Redis、SQLite 等存储实现名称，`Settings` 决定单个存储实例的连接和数据作用域。每个 Broker Server 使用独立实例。外层 `Message` 保存 Identifier、Topic、Identity、Tags、Timestamp；私有二进制载荷保存业务 Data 和绝对 Expiration。

Broker 启动时枚举 Pending 并恢复投递。消息到达绝对过期时间时，从 Pending 删除并记录本地化诊断。

## 6. 配置

客户端运行快照包括 `Server`、`Port`、`Topic`、`Group`、`Client`、`Instance`、`Filter`、`Timeout`、`Heartbeat` 和 `ReconnectInterval`。

`Timeout` 用于发现、订阅同步与 Control 接纳结果等待；`Heartbeat` 控制心跳；`ReconnectInterval` 控制重新发现节流。运行中修改原设置对象不会改变既有 Queue。

服务端三段配置按 `Control,Incoming,Outgoing` 排列，默认为 `32100,32101,32102`；两段配置仍表示 `Incoming,Outgoing` 且 Control 配置值为零，挂载 Storage 时随机绑定 Control 端口。三个端口均做范围与冲突校验。生产环境建议固定运行端口以简化防火墙配置。所有 TCP 端点默认绑定外部接口，驱动不提供认证或加密，必须由可信网络边界保护。

## 7. 验收要点

- MostOnce：无订阅即时 `null` 且不追发；有订阅返回唯一标识且与接收消息一致；多订阅广播、Group、前缀、空载荷、取消订阅和并发发布。
- LeastOnce：无订阅不落盘；Storage 写入完成后立即成功；竞争消费；同标识重投；任一 ACK 删除；消费者离线与替换；Broker 重启恢复；过期清理；Storage 异常隔离。
- 生命周期：断线、随机端口重发现、Epoch 失效、背压暂停恢复、关闭期间拒绝命令和 Socket 在线程内释放。
- 回归：Requester/Responder、事件通道、实例过滤和逻辑 Group Topic。
- 验证：net8.0、net9.0、net10.0 构建与真实 Socket 测试串行通过；资源键、CRLF、Tab 和链接检查通过。

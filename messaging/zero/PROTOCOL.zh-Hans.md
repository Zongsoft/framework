# Zongsoft.Messaging.ZeroMQ 协议

本文档定义当前 ZeroMQ 驱动使用的、不兼容旧版的 `1.0` 线协议。所有文本帧均使用 UTF-8，协议字段名和命令字区分大小写。客户端与 Broker 不支持不同协议版本混用。

## 1. 拓扑

| 子系统 | 客户端 Socket | Broker Socket | 用途 |
| --- | --- | --- | --- |
| 发现 | REQ | REP | 发现 Broker 代次及运行端口 |
| Broadcast | XPUB / SUB | XSUB / XPUB | `MostOnce` 广播 |
| Control | DEALER | ROUTER | `LeastOnce` 注册、接纳、投递与确认 |

Control 端点只有在 Broker 配置 `IMessageStorage` 后才启用。发现、Broadcast 和 Control 相互独立；Control 不承载请求响应适配器的业务语义。

## 2. 发现协议

请求为一个文本帧：

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Command:Discover
Instance:<client-instance>
```

成功响应为一个文本帧。启用 Control 时 `Ports` 按 `Control,Incoming,Outgoing` 排列：

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>
Ports:<control>,<incoming>,<outgoing>
```

未启用 Control 时省略 Control 端口：

```text
Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>
Ports:<incoming>,<outgoing>
```

客户端只接受两个或三个端口值。Broker 重启会生成新的 `Epoch`，客户端据此使已有 Broadcast 订阅状态失效并重新连接。

## 3. Broadcast 协议

### 3.1 Welcome

Broker XPUB 使用 Welcome 帧同步订阅者所连接的 Broker 代次：

```text
\0Zongsoft.Messaging.ZeroMQ
Protocol-Version:1.0
Epoch:<broker-epoch>\0
```

### 3.2 业务消息

Broadcast 业务消息固定为两帧：

```text
Frame 0:
<physical-topic>
Protocol-Version:1.0
Identifier:<message-id>
Identity:<producer-instance>
Tags:<tags>                    # 可选
Compression:<algorithm>       # 可选

Frame 1:
<payload>
```

`physical-topic` 包含可选的 `Group:` 前缀；订阅端在构造 `Message` 前移除该前缀。头字段值不得包含 CR 或 LF。`Tags` 可以包含逗号、分号和冒号。未压缩时不发送 `Compression`；压缩只作用于第二帧的业务负载。

心跳仍是匿名空载荷消息，不包含 Identifier、Identity、Tags 或 Compression。带 Identifier 的空载荷是普通业务消息。

Broker 的应用 XPUB 在发布瞬间没有匹配订阅时，客户端返回 `null`，不会等待未来订阅者，也不会发送该消息。

## 4. Control 协议

ROUTER 路由帧不在下表的固定帧计数中；客户端 DEALER 发送或接收的帧如下。

### 4.1 注册与保活

```text
REGISTER   Session, Subscription, Topic
REGISTERED Subscription
UNREGISTER Session, Subscription
PING       Session, Subscription
```

### 4.2 发布接纳

```text
PUBLISH Identifier, Topic, Identity, Tags, Timestamp, Expiration, Compression, Data
ACCEPTED Identifier
UNROUTABLE Identifier
ERROR Code, Identifier
```

`Timestamp` 和非零 `Expiration` 是 UTC ticks。未压缩时 Compression 帧为空。Broker 只校验算法名称并原样持久化 Data，不解压普通业务负载。

没有在线匹配订阅时 Broker 返回 `UNROUTABLE` 且不持久化。存在匹配订阅时，Broker 必须先完成 Pending 持久化，再返回 `ACCEPTED`。

### 4.3 投递与确认

```text
DELIVER Subscription, Identifier, Topic, Identity, Tags, Timestamp, Attempt, Compression, Data
ACK     Session, Subscription, Identifier
```

订阅端按 Compression 解压 Data 后构造 `Message`。Broker 在在线订阅者间竞争投递；未确认时沿用相同 Identifier 重投，任一有效 ACK 使消息停止投递并进入异步删除流程。

## 5. 持久化载荷

Broker 的 Storage 外层记录使用 `Message.Identifier`、`Topic`、`Identity`、`Tags` 和 `Timestamp`。私有 Data 使用当前协议版本序列化以下内容：

- `Version`：固定为 `1.0`；
- `Compression`：压缩算法名称，未压缩时为空；
- `Data`：压缩后的业务负载；
- `Expiration`：绝对过期时间。

Broker 不读取旧版本持久化数据。部署协议 `1.0` 前应清空或切换原 Pending Storage。

## 6. 限制与错误处理

- Header 最大 16 KiB，Topic 最大 1024 字节，Identifier 最大 256 字符，Payload 最大 64 MiB。
- Broadcast 必须恰好包含两个业务帧；Control 命令必须符合固定帧数。
- 受支持的负载算法为 Brotli、GZip、ZLib 和 Deflate，名称匹配忽略大小写。
- 畸形帧、未知算法或损坏的压缩载荷只丢弃当前消息并记录诊断，不得终止 Poller。
- `MostOnce` 的非空返回值只表示本地发送；`LeastOnce` 的非空返回值只表示 Broker 已持久接纳，均不表示 Handler 已执行。

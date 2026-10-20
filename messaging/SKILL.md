---
name: zongsoft-messaging
description: 实现、审查、调试或测试 framework/messaging 下的消息队列驱动与可靠消息存储。适用于 Core 消息契约、发布订阅、Topic/Group/Tags、可靠性、显式确认、压缩、重连、能力声明、驱动一致性和集成测试；ZeroMQ 线协议细节由其专用技能处理。
---

# Zongsoft 消息驱动工作流

## 定位顺序

1. 阅读 [AGENTS.md](AGENTS.md)、目标驱动 README、插件配置和测试。
2. 阅读 `../Zongsoft.Core/src/Messaging` 中直接相关的 Queue、Consumer、Message、Options、Reliability 和 Feature 契约。
3. 对比至少一个语义相近的其它驱动，区分公共行为和 Broker 特性。
4. ZeroMQ 工作使用 [zero/SKILL.md](zero/SKILL.md)；数据库消息存储读取 [.storages/AGENTS.md](.storages/AGENTS.md)。

## 配置与调用链

- 服务扫描注册 `IMessageQueueProvider`；业务消费者按提供者 Name 匹配，然后调用 `Queue(connectionName)`，而不是构造 KafkaQueue/RabbitQueue 等实现。
- [MessageQueueProviderBase](../Zongsoft.Core/src/Messaging/MessageQueueProviderBase.cs) 从 `/Messaging/ConnectionSettings` 按连接名和驱动名取类型化设置，按名以弱引用复用未释放的队列。附加 settings 会修改取得的配置对象，不能假定每次 Queue 调用都生成隔离副本。
- [MessageQueueBase](../Zongsoft.Core/src/Messaging/MessageQueueBase.cs) 拥有公共生产/订阅边界，驱动实现负责 SDK 回调到 Core Message 的适配、关闭与确认。核对返回 Consumer 的所有权，不逐次释放应用共享队列。
- 驱动 `.plugin` 注册设置驱动、提供者与扩展节点；ZeroMQ daemon 清单才启用 Broker 等工作器。消息存储是另一可选依赖，不等于所有消息自动可靠。
- 数据库存储实现位于 [.storages](.storages)，Redis 存储位于 [externals/redis](../externals/redis)。核对存储标识冻结时点、Broker 名与连接匹配、快照和历史分区兼容性。

## 连接设置（ConnectionSettings）

- 先读 [Core 设置契约](../Zongsoft.Core/docs/settings.zh-Hans.md)。Kafka、MQTT、RabbitMQ 和 Redis 带 `TOptions`，主参数以驱动的 `Descriptors` 为准；ZeroMQ 和阿里云 MQTT 不带此约束，不能把“未知文本项进入 `Properties`”套用到所有消息驱动。
- Kafka 使用 `GetProducerOptions()` / `GetConsumerOptions()`；MQTT、RabbitMQ、Redis 使用各自的 `GetOptions()`。`Value` 是完整框架设置文本，扩展项不是 SDK 配置透传入口。
- 提供者 `Queue(name, settings)` 的附加参数逐项写主字典，遵守该驱动的键约束；它不会将未知键自动转存 `Properties`，也不会重新配置已复用的队列。

## 必须明确的语义

- Topic、Group、Tags、Filter 和实例身份如何映射到物理 Broker。
- `ProduceAsync` 何时完成、载荷何时必须快照、返回标识代表什么。
- `MostOnce`、`LeastOnce`、`ExactlyOnce` 实际支持程度，以及失败/重复投递边界。
- `Message.AcknowledgeAsync` 如何映射到 commit/ack；Handler 返回不得自动推导为确认，除非 Core 统一改变契约。
- 同时核对 SDK 后台行为：Kafka 当前构造 ConsumerConfig 没有关闭自动提交，显式 Acknowledge 映射 Commit 不等于“未确认绝不提交”。不要仅从 Message 包装推断端到端至少一次保证。
- 压缩、延迟、过期、优先级等 Feature 只在完整实现并测试后声明。
- 连接、Channel、Socket、Poller 和回调线程的所有权，以及关闭时如何停止分派并释放。

## 异步生命周期

队列、轮询器和订阅消费者通过异步入口完成初始化、关闭及释放。SDK 的同步回调只负责接收和转交通知；异步处理必须由可等待的任务或有界队列承接。停止时禁止新通知、等待已接受处理结束，再释放自有连接和同步资源。处理、溢出和清理故障必须可观察，不使用 `async void` 或同步等待异步任务。

确认调用使用 `Message.AcknowledgeAsync` 并传递取消；确认完成前不能报告业务确认成功。测试需覆盖慢处理器、关闭期间的在途操作、重复释放和初始化失败清理。

## 测试

- 先构建 Core 和目标驱动，再运行网络无关测试。
- 集成测试使用项目声明的环境变量和本地 Broker；先探测 Ready，不默认运行 pod 脚本。
- 覆盖首条消息、空载荷、并发发布、分组竞争、显式确认、断连重连、取消订阅、关闭竞态和不支持能力。
- 使用唯一 Topic/Group 隔离测试，不清理共享 Broker 中非本测试数据。

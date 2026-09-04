## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`messaging` 包含 Core 消息契约的具体队列驱动，以及独立的可靠消息存储实现。

## 工作边界

- 先阅读 `Zongsoft.Core/src/Messaging` 中相关契约，再修改驱动；跨驱动共同行为才提升到 Core。
- 每个驱动负责连接设置、主题/分组映射、能力声明、发布订阅、确认和资源释放。
- 明确区分消息被本地客户端接受、发送到 Broker、Broker 持久化、消费者收到和业务确认。
- `MessageQueueFeature` 必须与真实支持一致；不要以第三方客户端的默认行为代替框架契约。
- 插件、选项、部署文件和 README 应与连接驱动、服务端组件及可选存储保持同步。
- 通用驱动工作流见 [SKILL.md](SKILL.md)，ZeroMQ 的额外协议约束见 [zero/SKILL.md](zero/SKILL.md)。

## 验证

- 先构建 Core，再构建目标驱动解决方案；运行相关测试前确认外部 Broker 或测试开关。
- 消息测试覆盖发布/订阅、取消订阅、确认、重连、并发、空载荷和关闭释放。
- 不默认启动或停止 Kafka、RabbitMQ、MQTT 等容器。

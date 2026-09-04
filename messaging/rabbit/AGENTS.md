## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目将 Core 消息队列契约适配到 RabbitMQ 的 Exchange、Queue 和 Consumer。

## 工作边界

- 保持 Group 到 Exchange/Queue、Topic 到 routing key、Tags 到 consumer tag/headers 的既有映射。
- 当前驱动声明压缩能力并使用显式消费确认；不要在 Handler 完成前确认消息。
- Channel/Connection 的线程安全和恢复语义必须清晰，关闭时取消消费者并释放通道。
- 可靠性、持久化、路由和重试必须依据当前驱动实现，不从 RabbitMQ 概念推断框架未声明能力。

## 验证

- 构建 `Zongsoft.Messaging.RabbitMQ.slnx`。
- 集成测试前检查 `Zongsoft.Messaging.RabbitMQ-pod.yaml` 对应服务；不默认运行 pod 脚本。
- 覆盖路由、分组、确认/拒绝、连接恢复、取消订阅和并发发布。

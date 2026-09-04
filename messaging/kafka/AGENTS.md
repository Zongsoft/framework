## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目将 Core 消息队列契约适配到 Apache Kafka。

## 工作边界

- 保持 Topic、Group、Key、Header/Tags 与 Kafka 消费组语义的映射一致。
- `Message.AcknowledgeAsync` 对应消费位点提交；不要在处理器确认前提前提交，也不要把处理器正常返回视为隐式确认。
- Kafka 默认可靠性、压缩能力和连接属性必须与 `KafkaQueue.Features` 及选项文件一致。
- Confluent 客户端对象的线程安全、轮询循环、取消和释放按其所有权边界处理。

## 验证

- 构建 `Zongsoft.Messaging.Kafka.slnx`。
- 集成测试前检查 `Zongsoft.Messaging.Kafka-pod.yaml` 对应服务和测试配置；不要未经要求运行 pod 启停脚本。
- 覆盖分组竞争、显式确认、停止后不再分派、重连和并发发布。

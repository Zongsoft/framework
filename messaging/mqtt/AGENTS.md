## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目既可连接 MQTT Broker，也包含可选的嵌入式 `MqttQueueServer`。

## 工作边界

- 保持 `MessageReliability` 与 MQTT QoS 0/1/2 的映射，以及 Topic、Group、Tags 和压缩语义。
- 客户端接收关闭自动应答，`Message.AcknowledgeAsync` 必须驱动对应 MQTT 确认；异常和取消路径不得错误确认。
- 区分客户端与嵌入式服务端生命周期，关闭时停止分派并释放会话、订阅和后台任务。
- 修改服务端配置时同步插件、选项、部署文件和样例。

## 验证

- 构建 `Zongsoft.Messaging.Mqtt.slnx` 并运行相关测试。
- 分别验证 QoS、显式确认、重复消息、断线重连、服务端启停和空载荷；外部 Broker 不可用时说明限制。

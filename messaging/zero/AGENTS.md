## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目是基于 NetMQ 的 ZeroMQ 队列、请求响应和事件传输实现，包含广播与可靠控制协议。

## 工作边界

- 修改前阅读 [SKILL.md](SKILL.md)、[PROTOCOL.zh-Hans.md](PROTOCOL.zh-Hans.md) 和相关实现/测试。
- Socket 必须归属单一 Poller/Actor 线程；跨线程操作通过队列、通道或 Actor 命令完成。
- 严格区分 `MostOnce` 与 `LeastOnce`，不声称支持 `ExactlyOnce`；可靠确认必须显式调用 `AcknowledgeAsync`。
- 线协议、Broker Epoch、订阅可见性、持久化、重试和消息快照属于兼容性契约。
- 插件、daemon 插件、Storage 插件、选项和部署文件应与服务端能力同步。

## 验证

- 依次构建 Core 和 ZeroMQ 的目标框架；按技能说明运行 opt-in 集成测试。
- NetMQ 使用进程级状态和网络资源，多目标测试串行执行，并覆盖启动、重连、并发和确定性释放。

## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目是进程外 Native AOT 部署器，消费 upgrader 生成的 `.deployment`；详细约束见 [SKILL.md](SKILL.md)。

## 工作边界

- 保持等待宿主退出、锁定描述文件、完整/增量复制、执行器事件、清理描述文件和重启的顺序。
- 完整部署不得删除 `.deployer` 自身；启动器按平台和应用类型选择。
- 新依赖必须兼容 trimming、Native AOT、自包含和单文件发布。
- 真实服务启动、IIS 回收、systemd/sc 操作和应用目录部署不得作为默认测试副作用。

## 验证

- 构建 `Zongsoft.Upgrading.Deployer.slnx`。
- 文件行为使用临时应用目录覆盖 Fully/Delta、锁、失败恢复和清理；发布变化再验证目标 runtime 的 AOT 流程。

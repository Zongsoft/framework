## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目通过 `Zongsoft.Data` 和命名命令为可靠消息 Broker 提供数据库存储，不属于任何单一消息驱动。

## 工作边界

- 保持 `IMessageStorage` 的消息快照、标识、主题、标签、时间戳和清理语义完整。
- 公共 `.mapping` 定义命令及参数，驱动 SQL 位于 `scripts/{driver}`；修改一端时核对另一端和 `database` 建表脚本。
- 数据连接名称与 Broker 名称的匹配规则不得静默回退到无关数据源。
- 插件、选项和部署文件必须注册正确工厂，不能把具体数据库 Provider 包引入公共存储程序集。

## 验证

- 构建 `Zongsoft.Messaging.Storages.slnx` 并运行聚焦测试。
- SQL 变化至少校验受影响驱动脚本的参数名、读写语义和映射；需要数据库时先确认服务就绪。

## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。每个子目录是独立数据库驱动，负责提供程序连接、SQL 方言、绑定、导入和驱动插件产物。

## 工作边界

- 先判断行为是通用引擎契约还是单一数据库差异；单一差异不得下沉到公共接口。
- 沿用目标驱动现有 StatementBuilder、Visitor、Binder、Slotter 和 Importer 扩展方式。
- Provider 包版本由根 `Directory.Packages.props` 管理；多目标框架的传递依赖冲突应按目标框架分析。
- 驱动插件、部署文件、连接设置和 README 必须与实现保持一致。
- SQLite、DuckDB 为进程内数据库；其它驱动测试通常需要对应 `*-pod.yaml` 或外部数据库，运行前动态确认。

## 验证

- 构建目标驱动的 `.slnx`，再运行其 `test` 项目；不要默认运行所有数据库集成测试。
- 数据库服务 Running 不等于 Ready，应先探测连接，并为失败测试设置合理超时。
- 变更通用行为时先验证 Data/Core，再抽取至少一个相关驱动做回归。

## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。每个子目录是独立数据库驱动，负责提供程序连接、SQL 方言、绑定、导入和驱动插件产物。

## 工作边界

- 先判断行为是通用引擎契约还是单一数据库差异；单一差异不得下沉到公共接口。
- 沿用目标驱动现有 StatementBuilder、Visitor、Binder、Slotter 和 Importer 扩展方式。
- 通用包版本由根 `Directory.Packages.props` 管理；非通用及第三方 Provider 包版本在驱动 `.csproj` 中使用 `VersionOverride` 定义。多目标框架的传递依赖冲突应按目标框架分析。
- 驱动插件、部署文件、连接设置和 README 必须与实现保持一致。
- SQLite、DuckDB 为进程内数据库；其它驱动测试通常需要对应 `*-pod.yaml` 或外部数据库，运行前动态确认。

- 数据库扩展点使用异步契约，逐行导入须传递 MemberValue.Target；提供程序同步边界和 TDengine 私有适配器的所有权见 [数据引擎技能](../SKILL.md#驱动异步边界)。

## 验证

- 构建目标驱动的 `.slnx`，再运行其 `test` 项目；不要默认运行所有数据库集成测试。
- 仅运行离线配置或 SDK 替身测试时使用明确的类过滤器；同一测试项目还包含真实数据库用例，不能无过滤器运行。
- 数据库服务 Running 不等于 Ready，应先探测连接，并为失败测试设置合理超时。
- 真实数据库测试须显式设置 `ZONGSOFT_DATA_TESTS=1`，并分别记录 net8.0、net9.0、net10.0 结果；未开启开关的提前返回不能作为数据库通过证据。跳过数按测试结果条目的 NotExecuted 统计，不能只读取 TRX 汇总 Counters.notExecuted。
- 变更通用行为时先验证 Data/Core，再抽取至少一个相关驱动做回归。

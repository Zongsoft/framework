## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Diagnostics` 提供诊断配置、日志与 OpenTelemetry 数据接收/输出能力。

## 工作边界

实现流程、源码路由与跨项目风险见 [SKILL.md](SKILL.md)；使用者教程维护在双语 README 中。

- 主库负责框架诊断能力，`protocols/client` 与 `protocols/server` 负责协议传输，详细规则见 [protocols/AGENTS.md](protocols/AGENTS.md)。
- 保持 Activity、Metric、Log 数据映射、资源属性、批处理和生命周期语义稳定。
- 修改配置或插件入口时同步 `.option`、`.plugin`、`.deploy` 和 README。
- `proto` 是外部 Git 子模块；除非任务明确要求更新上游协议版本，否则不修改其内容或指针。

## 验证

- 构建 `Zongsoft.Diagnostics.slnx` 前先构建 Core；该解决方案和 Cake 默认 `test` 任务只包含单元测试。协议变化再构建对应 client/server 解决方案。
- 协议集成测试及其解决方案位于各自的 `test/integration` 目录，入口为 `protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx` 和 `protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx`。先按[服务端本地验证](protocols/server/README.zh-Hans.md#本地验证)生成程序集依赖，再显式运行集成测试；Cake 的 `integration-test` 任务会构建样例和集成测试项目。
- 网络或导出器集成测试应使用可控端点，不能把发送成功等同于后端已持久化。

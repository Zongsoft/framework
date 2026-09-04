## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Diagnostics` 提供诊断配置、日志与 OpenTelemetry 数据接收/输出能力。

## 工作边界

- 主库负责框架诊断能力，`protocols/client` 与 `protocols/server` 负责协议传输，详细规则见 [protocols/AGENTS.md](protocols/AGENTS.md)。
- 保持 Activity、Metric、Log 数据映射、资源属性、批处理和生命周期语义稳定。
- 修改配置或插件入口时同步 `.option`、`.plugin`、`.deploy` 和 README。
- `proto` 是外部 Git 子模块；除非任务明确要求更新上游协议版本，否则不修改其内容或指针。

## 验证

- 构建 `Zongsoft.Diagnostics.slnx`；协议变化再构建对应 client/server 解决方案。
- 网络或导出器集成测试应使用可控端点，不能把发送成功等同于后端已持久化。

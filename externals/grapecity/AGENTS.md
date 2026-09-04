## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。本项目适配 GrapeCity 报表运行时，并通过 `api` 提供设计和报表 Web 能力。

## 工作边界

- `src` 封装报表引擎和设计能力，`api` 只负责 HTTP 接入与资源转换。
- 保持报表定义、参数、资源、导出格式和流生命周期兼容 Zongsoft.Reporting。
- 授权文件和许可信息属于敏感环境资产，不提交、不复制到日志或测试产物。
- API、插件和部署变化需同步检查主库与 Web 项目。

## 验证

- 构建 `Zongsoft.Externals.Grapecity.slnx`；Web 变化同时验证 api 项目。
- 需要商业运行时或许可证时明确环境限制，使用脱敏的最小报表验证。

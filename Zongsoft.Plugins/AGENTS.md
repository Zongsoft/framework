## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Plugins` 实现插件树解析、构建、装配、宿主生命周期、服务与配置集成，是插件化应用的核心。

## 工作边界

- 以 `Zongsoft.Plugins.xsd`、`plugins/*.plugin` 和解析器当前行为共同核对插件 XML 契约。
- 保持插件路径、依赖顺序、节点构建、服务解析、应用启动/停止和卸载语义稳定。
- 不把具体业务组件硬编码进框架；通过构建器、解析器、服务或配置扩展点接入。
- 触碰 ASP.NET 宿主集成时同时检查 [../Zongsoft.Plugins.Web/AGENTS.md](../Zongsoft.Plugins.Web/AGENTS.md)。
- 插件格式和装配工作使用 [SKILL.md](SKILL.md)。

## 验证

- 构建 `Zongsoft.Plugins.slnx`。
- 使用 `plugins/Main.plugin`、`Terminal.plugin` 或聚焦测试夹具验证解析、依赖和生命周期；不要以真实业务宿主作为首选测试环境。

- 主解决方案和默认 `build.cake` 的 `test` 目标只包含 `test/unit`。宿主生命周期、子进程和 Web 端点用例位于 `test/integration`，通过独立解决方案和 `integration-test` 目标显式运行。

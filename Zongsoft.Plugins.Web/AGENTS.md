## 概述

本目录遵循 [../AGENTS.md](../AGENTS.md)。`Zongsoft.Plugins.Web` 将插件应用生命周期、服务与 ASP.NET Core Web 宿主连接起来。

## 工作边界

- 保持通用插件装配在 `Zongsoft.Plugins`，本项目只承载 Web 宿主、控制器发现和 ASP.NET 集成。
- 修改控制器激活、服务作用域或应用启动顺序时同时核对 ASP.NET 生命周期与插件卸载行为。
- 不在宿主桥接层加入具体站点或业务规则。
- 插件装配规则参见 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)。

## 验证

- 构建 `Zongsoft.Plugins.Web.slnx`。
- 涉及控制器或路由发现时，配合最小 Web 宿主验证注册结果和启停释放，不默认启动外部业务应用。

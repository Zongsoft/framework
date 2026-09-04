---
name: zongsoft-plugins
description: 开发、审查、调试或测试 Zongsoft.Plugins 与 Zongsoft.Plugins.Web，包括 .plugin XML 解析、插件树、构建器、组件装配、服务解析、宿主生命周期、Web 控制器发现和部署产物；不用于业务插件自身的领域逻辑。
---

# Zongsoft 插件框架

## 入手顺序

1. 阅读 [AGENTS.md](AGENTS.md)、README 和 `Zongsoft.Plugins.xsd`。
2. 查找 `plugins/Main.plugin`、`Terminal.plugin` 及同类型解析器/构建器作为可执行样例。
3. 确认问题属于 XML 语法、插件树、组件构建、服务解析、应用生命周期，还是 Plugins.Web 的 ASP.NET 桥接。

## 配置契约

- `.plugin` 的节点名称、属性、路径、依赖和顺序可能影响装配；保持局部格式，不做无关排序。
- 解析规则应与 XSD 和现有容错行为一致。若二者不一致，先以加载器测试确认兼容边界并记录差异。
- 插件依赖描述运行时装载顺序；不要以项目编译成功替代插件可加载性验证。
- `.option`、`.mapping`、`.deploy` 分别属于配置、数据和部署契约，不把其语义混入插件解析器。

## 生命周期与 Web

- 组件创建、初始化、启动、停止、卸载和释放顺序必须确定；失败回滚不能留下已注册服务或后台任务。
- 服务解析应尊重作用域、具名服务和插件容器边界，避免隐式全局单例。
- Plugins.Web 只桥接 ASP.NET Hosting、DI 和控制器发现；业务路由与控制器实现留在业务插件。
- 修改 Web 激活或作用域时同时验证请求作用域结束、应用停止和插件卸载。

## 验证

先构建 `Zongsoft.Plugins.slnx`；涉及 Web 再构建 `../Zongsoft.Plugins.Web/Zongsoft.Plugins.Web.slnx`。使用最小插件树覆盖有效配置、缺失依赖、循环/无效节点、初始化失败回滚和确定性释放。

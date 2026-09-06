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

## 源码路由与实际时序

1. [ApplicationBuilder](src/Hosting/ApplicationBuilder.cs) 添加 PluginConfigurationSource，加载插件树，扫描已加载的入口引用、入口程序集和插件 Manifest.Assemblies，再构建宿主。
2. [PluginLoader](src/PluginLoader.cs) 按文件目录组织父子插件、解析依赖；文件系统层级与 extension path 分离。不要把注释中的排序描述当作确定的文件枚举顺序。
3. [PluginApplicationContext](src/PluginApplicationContext.cs) 初始化后挂载应用上下文；首次取得 Workbench 构建工作台子节点，Startup 子树后置，随宿主 Started/Stopping 打开/关闭工作台。
4. [ObjectBuilder](src/Builders/ObjectBuilder.cs)、[BuilderManager](src/Builders/BuilderManager.cs) 处理节点构造和缓存；[ServicesParser](src/Services/ServicesParser.cs) 根据属性类型与当前树节点上下文定位服务。
5. [PluginConfigurationProvider](src/Configuration/PluginConfigurationProvider.cs) 只关联已加载清单同主名的 option 及环境/host/site 附属文件。跨插件提供程序使用并发字典枚举，不承诺冲突键的跨插件覆盖顺序。

### 易混淆契约

- `{service:@}` 返回应用容器，`{service:@Orders}` 返回已挂载 Orders 模块容器；`~` 按目标成员类型取得服务，`*` 取得集合。具名服务别名与模块名称不是同一概念。
- Core 的 `Locate<T>("Orders@Redis")` 选择供应具名实例的服务提供者，不能翻译成 `{service:Orders@Redis}`。
- 基础解析器/构建器由 [Main.plugin](plugins/Main.plugin) 注册；仅拷贝业务 DLL 不会补齐这些装配依赖。
- 配置 XML 的文本节点是集合项，标量用属性；加载配置后断言实际键和值，不能只验证 XML 可解析。
- 当前服务特性扫描默认单例。README 消费示例优先使用契约和容器，不手工 new 每个实现，也不逐次 Dispose 共享服务。

## 生命周期与 Web

- 修改组件创建、启动/停止、卸载时分别验证失败清理；不要宣称现有所有构造失败均有事务性回滚。依赖初始化与插件节点构建是不同阶段。
- 服务解析应尊重作用域、具名服务和插件容器边界，避免隐式全局单例。
- Plugins.Web 的 [Application](../Zongsoft.Plugins.Web/src/Application.cs) 初始化上下文、执行 `IApplicationInitializer<IApplicationBuilder>`，再装配中间件与 Controller/Hub 端点；[WebApplicationBuilder](../Zongsoft.Plugins.Web/src/WebApplicationBuilder.cs) 配置服务和默认策略。业务路由与控制器实现留在业务插件。
- 修改 Web 激活或作用域时同时验证请求作用域结束、应用停止和插件卸载。

## 验证

先构建 `Zongsoft.Plugins.slnx`；涉及 Web 再构建 `../Zongsoft.Plugins.Web/Zongsoft.Plugins.Web.slnx`。使用最小插件树覆盖有效配置、缺失依赖、循环/无效节点、初始化失败回滚和确定性释放。

获得宿主运行授权后可在独立部署目录验证；不能把真实业务部署树原样启动作为最小测试。Windows 终端需 PTY/ConPTY，默认内容根受工作目录影响，复制输出保留 runtimes。检查插件列表后必须继续验证契约调用：发现条目不等于 SDK 版本、配置和网络均正确。DLL 更新重启宿主，不把配置变更通知解释为程序集热替换。

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
- XSD 允许零个或多个 extension；manifest、builders、parsers 各自可省略且最多一个，各节按 manifest → builders → parsers → extension 排列。builders 出现时必须至少包含一个 builder，parsers 出现时必须至少包含一个 parser。模式声明 object/expose、构造及属性行为；动态属性使用 skip，动态子元素使用 lax，保留已知元素的结构校验。ReportValidationWarnings 对动态成员名给出的缺失模式提示不等于结构错误，不得通过把整个子树改成 skip 来隐藏。自定义构建器可以附加模式声明并复用 BuiltinElementType。上下文、类型、表达式及依赖仍由运行时检查；读取器未主动校验各节次数及顺序。用 PluginSchemaTests 分别验证模式约束和合法清单的加载行为。
- 插件依赖描述运行时装载顺序；不要以项目编译成功替代插件可加载性验证。
- `.option`、`.mapping`、`.deploy` 分别属于配置、数据和部署契约，不把其语义混入插件解析器。

## 源码路由与实际时序

1. [ApplicationBuilder](src/Hosting/ApplicationBuilder.cs) 添加 PluginConfigurationSource，加载整棵插件树及其配置，扫描已加载的入口引用、入口程序集和插件 Manifest.Assemblies，最后构建宿主。全部来源共用去重集合，每次宿主构建中同一程序集只注册一次；注册器可读取子插件的最终配置。Core 的 FeatureProvider.Default 内置事务环境提供者，插件通过 IFeatureProvider 服务追加条件，环境默认值不依赖程序集服务扫描。
2. [PluginLoader](src/PluginLoader.cs) 按文件目录组织父子插件、解析依赖；文件系统层级与 extension path 分离。不要把注释中的排序描述当作确定的文件枚举顺序。
3. [PluginApplicationContext](src/PluginApplicationContext.cs) 初始化后挂载应用上下文；首次取得 Workbench 构建工作台子节点，Startup 子树后置，随宿主 Started/Stopping 打开/关闭工作台。
4. [ObjectBuilder](src/Builders/ObjectBuilder.cs)、[BuilderManager](src/Builders/BuilderManager.cs) 处理节点构造和缓存；[ServicesParser](src/Services/ServicesParser.cs) 根据属性类型与当前树节点上下文定位服务。
	- 具体构建器在 Build 中设置 context.AppendToOwner 和 context.BuildChildren；管理器随实例保存决定，缓存路径沿用。IAppender 由所有者构建器提供，不是子节点追加自身的许可。不要在 IBuilder 增加固定策略查询或让调用方的 settings 积累流程标志。
	- IBuilderLifecycle 是独立可选能力，由 BuilderBase 显式实现并转发到受保护的 OnBuilding、OnBuilded。OnBuilding 在重入保护建立后、实际 Build 前调用；OnBuilded 在所需子节点装配后、所有者追加前调用，先应用声明行为。缓存复用不重放，延后完成不重复前置回调。BuilderSettings 只提供 TargetType。
	- 模块定位只取得实例，创建时调用 OnBuilding，后续正常装配时调用 OnBuilded；两次回调可能使用不同上下文。IsBuilded 在装配完成后才成立。普通 Auto 必须补完未完成节点；部分失败不得作为成功缓存返回。完整架构、装配时序与生命周期边界见 [implementation.zh-Hans.md](implementation.zh-Hans.md)。
5. [PluginConfigurationProvider](src/Configuration/PluginConfigurationProvider.cs) 先查找与 `.plugin` 文件同名的 `.option`，缺失时才尝试插件 name 对应的 `.option`，只加载第一个存在的入口；两者都缺失则结束，首选文件解析失败不回退。不扫描附属后缀，其他片段由入口通过 `<?import ...?>` 显式导入。宿主 LoadConfiguration(name) 同样只加载 name.option。无参 LoadConfiguration() 先加载应用名配置，入口程序集名不同时再追加加载其同名配置；Application.Web 在此之后加载 web.option，同名键由后加载的文件覆盖。显式 Load 重新选择入口并递归包括子插件；入口及导入文件重载、插件加载/卸载、整体重载均向宿主传递变更通知，替换或释放时解除订阅。整体重载失败清理已建立的提供程序。内部同步保护提供程序的发布、清理与释放，不覆盖整次 Load 和所有树事件；调用方应串行执行显式重载、加载/卸载与释放，通知不代表跨文件事务。
6. 跨插件提供程序使用并发字典枚举，不承诺冲突键的跨插件覆盖顺序。通过 Providers 取得各插件的 CompositeConfigurationProvider，其中单个 XML 提供程序包含入口及其导入后的生效值；文件来源标识入口，不追踪单键的导入来源。由宿主消费层比较各插件生效值并判断网络键等领域冲突，不把 Kestrel 或 gRPC 规则硬编码进 Plugins。

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

Plugins 测试只项目引用 Plugins，Core 使用 HintPath 程序集引用；Web 桥接及 Web 宿主用例归属 Plugins.Web 测试。Plugins.Web 测试只项目引用 Plugins.Web，Core、Plugins、Web 使用 HintPath 程序集引用，不通过项目引用构建它们。干净工作区按测试 README 先生成依赖程序集。

获得宿主运行授权后可在独立部署目录验证；不能把真实业务部署树原样启动作为最小测试。Windows 终端需 PTY/ConPTY，默认内容根受工作目录影响，复制输出保留 runtimes。检查插件列表后必须继续验证契约调用：发现条目不等于 SDK 版本、配置和网络均正确。DLL 更新重启宿主，不把配置变更通知解释为程序集热替换。

## Web 端点接入

Plugins.Web 的通用构建器不执行网络策略。现有服务扫描发现 Web 宿主的 Kestrel:Endpoints 子项，且尚未注册 WebEndpointCollection 时，由 WebEndpointExtension.Registration 自动调用 AddWebEndpoints(configuration) 和 AddWebEndpointMonitoring。非 Web 宿主或无端点声明时不接入；显式接入优先保留自选配置节和可选监视。接入后，由最终容器延迟捕获插件配置和协议注册、检查跨插件服务器同键异值、调用 KestrelUtility.Convert、以 PostConfigure 应用配置并适配 IPv6 TCP socket。扩展通过 Web 的 AddWebEndpointPipeline 安装私有启动过滤器，位于应用管线之前；不再包装监听工厂或禁止原生地址来源。AddWebEndpointMonitoring 在自动接入时启用重启提示，显式接入时可独立选择，启动订阅、停止/释放解除。详见 [Plugins.Web](../Zongsoft.Plugins.Web/README.zh-Hans.md#监听配置与请求范围)。hosting 入口无须修改；不在通用 Plugins 中增加网络规则。

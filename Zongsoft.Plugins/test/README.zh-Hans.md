# 插件重构回归基线

[English](README.md) | [简体中文](README.zh-Hans.md)

本项目验证插件装配的可观察行为，为后续重构提供回归基线。项目引用仅指向 Plugins，Core 通过 `HintPath` 引用本地程序集；本项目不依赖 Web。程序集引用不传递 NuGet 依赖，因此显式声明 Core 所需的 `Microsoft.Extensions.ObjectPool` 和 `Microsoft.Extensions.Caching.Memory` 运行时包。Web 桥接及宿主构建测试归属 [Plugins.Web 测试项目](../../Zongsoft.Plugins.Web/test/README.zh-Hans.md)。测试遵循 [framework 约定](../../AGENTS.md)和 [Zongsoft 开发规范](../../../guidelines/zongsoft.csharp.guidelines.md)，不修改生产类型的可见性，不断言框架本地化文案。

## 调查依据

2026-10-11 调查覆盖 `Zongsoft.Plugins`、`Zongsoft.Plugins.Web`，以及 Core 的服务容器、配置、命令和应用生命周期与 Web 的控制器发现/激活实现。扫描 `D:\Zongsoft`（包含本框架）及 `D:\Automao\server` 时，共读取 595 个 `.plugin` 文件，按 SHA-256 内容去重为 234 份，均能解析为 XML。数字包含当时已有的源码、构建和部署副本；不是 595 个独立业务插件，也不代表它们均通过 XSD 验证。

真实清单中的主要装配形态已转化为最小测试：主从依赖、目录层级、隐藏清单、同路径覆盖、`object`/`expose`、显式构造参数、属性行为、对象成员扩展、`../@成员`、静态字段及嵌套类型、选项与模块服务解析。业务清单没有原样启动，不需要数据库、消息队列、凭据或业务部署目录。测试运行时不依赖上述两个调查目录。

基础注册来自随产品交付的 [Main.plugin](../plugins/Main.plugin)：普通夹具移除其中的扩展节点，保留真实解析器和构建器注册；另有用例完整加载 Main 与 [Terminal.plugin](../plugins/Terminal.plugin)，验证清单和覆盖装配而不构建业务对象。

## 运行

在 framework 根目录执行。需要 .NET 10 SDK 及 .NET 8、9、10 运行时；沿用项目中央包版本和 VSTest + xUnit v3 运行方式。

首次在干净工作区运行时，先按顺序构建以下项目。生产项目的 Debug 配置使用相邻项目 DLL 的 `HintPath`，因此应先生成这些依赖。此基线针对当前源码的 Debug 构建；Release 的生产项目会引用已发布包，不能等同于当前源码验证。

```powershell
dotnet build ./Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet build ./Zongsoft.Plugins/Zongsoft.Plugins.slnx -p:GeneratePackageOnBuild=false

dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net8.0 --no-build --no-restore
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net9.0 --no-build --no-restore
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore
```

宿主子进程用例标记为 `Category=Integration`，仍然完全在本地运行。只运行单元测试时添加 `--filter 'Category!=Integration'`；只运行集成测试时添加 `--filter 'Category=Integration'`。子进程通过 PATH 中的 `dotnet` 启动当前目标框架的测试程序集，不打开交互终端、不启动 HTTP 监听；30 秒超时后只终止该测试创建的进程。

规范检查保持只读：

```powershell
$testFrameworkBeforeFormat = $env:TargetFramework
try {
	$env:TargetFramework = 'net10.0'
	dotnet format style ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj --no-restore --verify-no-changes --diagnostics IDE0049
}
finally {
	$env:TargetFramework = $testFrameworkBeforeFormat
}
git -c core.whitespace=cr-at-eol diff --check -- Zongsoft.Plugins
```

构建默认启用 `ZongsoftCodeStyleStrict`。格式检查临时指定单个框架，避免当前 SDK 在加载跨目标外层项目时报告缺少 Compile 目标的工作区警告；不改变构建与测试的三个目标框架。源码使用 Tab、CRLF 和 UTF-8 无 BOM，已有许可头保留。上面的 Git 临时参数使空白检查正确识别 CRLF，不修改仓库配置。

## 覆盖范围

| 测试文件 | 主要契约 |
| --- | --- |
| [PluginLoaderTests](PluginLoaderTests.cs) | 加载事件与幂等性、依赖先后、主从关系、目录/隐藏插件、重复名称、可选程序集、无效声明、覆盖节点和产品清单 |
| [PluginSchemaTests](PluginSchemaTests.cs) | 产品 XSD 和清单、无扩展插件、manifest/builders/parsers 各自可选且唯一、builders/parsers 出现时至少包含一个注册项、manifest → builders → parsers → extension 的顺序、1,365 种顶层节组合、嵌套 object/expose、动态属性、无名称的类型参数与文本参数、自定义模式、警告边界及非法已知结构 |
| [PluginTreeTests](PluginTreeTests.cs) | 路径、大小写、节点顺序、挂载事件、卸载保留子节点、只读集合和非法输入 |
| [ObjectBuilderTests](ObjectBuilderTests.cs) | 上下文/程序集/资源/模块服务注入、构造参数、成员赋值、类型推断、缓存、并发、失败恢复和释放 |
| [BuilderExtensionTests](BuilderExtensionTests.cs)、[ExposeBuilderTests](ExposeBuilderTests.cs)、[BuildPipelineTests](BuildPipelineTests.cs) | 所有者追加器、Build 的动态控制及缓存保持、集合转换/展开、expose 冷/热构建、属性回路、兄弟引用去重、模块延后装配、循环与失败诊断、设置复用 |
| [BuilderLifecycleTests](BuilderLifecycleTests.cs) | 可选生命周期接口、直接实现、回调顺序、属性行为、缓存/延后/模块构建、显式重新构建、空结果及回调失败 |
| [ParserTests](ParserTests.cs)、[PredicateParserTests](PredicateParserTests.cs)、[ResourceParserTests](ResourceParserTests.cs) | path/type/static/object/service/option/predicate/resource，服务别名、同步/异步断言、未知方案和无效表达式；资源只使用测试自有数据 |
| [CommandTests](CommandTests.cs) | List/Tree/Find、显式构建选项、延迟命令、参数和取消令牌传递、非法参数 |
| [PluginConfigurationTests](PluginConfigurationTests.cs)、[HostingConfigurationTests](HostingConfigurationTests.cs) | 插件按清单主名再逻辑名称选择精确入口、不扫描后缀文件且解析失败不回退、按文档顺序显式导入、每插件仅一个入口提供程序、宿主精确名称与物理/非物理文件提供程序、子插件重载、连续变更通知、入口来源追溯、失败重试与订阅释放 |
| [PluginModelTests](PluginModelTests.cs) | 选项身份、属性转换缓存、行为、基础元素继承与覆盖、依赖集合 |
| [PluginUnloadTests](PluginUnloadTests.cs) | 卸载取消、资源释放、节点/配置移除、已卸载入口及其导入内容不会转入名称有前缀关系的插件，以及未实例化基础元素不被创建 |
| [ApplicationLifecycleTests](ApplicationLifecycleTests.cs)、[TestProgram](TestProgram.cs) | 初始化、工作台状态/事件/重试、Startup 工作者筛选，隔离进程中的退出及 Daemon/Terminal 宿主构建；宿主与插件共享程序集只注册一次且能读取完整子插件配置 |

`PluginTestScope` 为每个用例建立唯一临时目录并释放配置提供程序及服务容器。框架的 `ApplicationContext.Current`、`BuilderManager.Current` 和插件树缓存是进程级状态，因此测试程序集禁用用例并行；并发构建用例仍显式发起并行调用。测试订阅的全局事件会解除。插件树缺少公开释放入口，测试不反射清空其静态缓存；缓存随测试进程结束释放。

卸载入口当前为 `internal`，仅 `PluginUnloadTests` 的一个辅助方法通过反射调用该入口，并断言公开可观察的结果；没有修改生产可见性或访问私有字段。重构此入口时只需调整该适配点。

## 覆盖率快照

2026-10-11，三个目标框架各 151 个用例通过，无跳过。net10.0 的 Coverlet 结果如下，包含生成的资源访问代码和当前未被使用的类，没有按低覆盖类排除统计：

| 程序集 | 行覆盖率 | 分支覆盖率 |
| --- | ---: | ---: |
| Zongsoft.Plugins | 79.67% | 70.47% |

Plugins 的主要类型行覆盖率：`PluginResolver` 92.35%、`PluginLoader` 84.40%、`BuilderManager` 93.90%、`ObjectBuilder` 83.76%、`PluginConfigurationProvider` 98.43%、`PluginApplicationContext` 95.65%、`WorkbenchBase` 98.80%。这些数字是执行覆盖指标，不等同于全部行为正确或重构安全。

重新生成报告：

```powershell
dotnet test ./Zongsoft.Plugins/test/Zongsoft.Plugins.Tests.csproj -f net10.0 --no-build --no-restore --collect "XPlat Code Coverage" --settings ./Zongsoft.Plugins/test/coverage.runsettings --results-directory ./Zongsoft.Plugins/test/TestResults
```

[coverage.runsettings](coverage.runsettings)只选择 Zongsoft.Plugins 程序集；报告写入被 Git 忽略的 `TestResults`。Core 相关代码会被测试调用，但不纳入这里的覆盖率。

## 重构前需单独解决的问题

测试记录当前装配契约，包括子插件配置重载、连续变更通知和程序集注册去重。以下清单区分保留行为与仍需单独回归的源码审查风险。

| 问题 | 证据与后续验证 |
| --- | --- |
| expose 缓存子节点追加 | 管理器检查 Build 保存的追加决定，再调用所有者构建器的 IAppender。冷/热路径均不追加 expose 自身；其子对象仍加入暴露的集合。BuildPipelineTests 验证动态构建条件及属性回路，BuilderLifecycleTests 验证延后完成顺序。见[设计架构与实现原理](../implementation.zh-Hans.md)。 |
| 成功关闭会退出进程 | 子进程用例已确认 `WorkbenchBase.Close` 经由 `ApplicationContext.Exit` 结束进程；`PluginApplicationContext.OnStopping` 先关闭工作台，再调用负责停止工作器的基类。应新增带启动/停止/释放记录的子进程回归，验证退出前清理顺序。 |
| 依赖环 | `PluginLoader.LoadPlugin` 递归推进依赖而没有环检测。应先在可超时、可隔离的进程中验证 A→B→A 和自依赖，再实现诊断；当前套件不触发可能的栈溢出。 |
| 深层卸载 | `PluginLoader.Unload` 在遍历子插件时移除同一集合，`PluginTree.UnmountItem` 也会在遍历子节点时移除节点。当前释放用例覆盖叶子构件，嵌套插件/构件与取消子卸载需单独回归。 |
| 多层相对路径 | `PluginTreeNode.Find` 遇到 `..` 使用最初节点的 `_parent`，而不是当前迭代节点的父级；需补 `../..`、`Child/..` 的期望路径测试。 |
| 相等性与哈希 | `Builtin` 相等比较忽略大小写，但哈希未采用相同规则；空 `PluginTreeNode` 的相等性不含树实例而哈希包含树实例。应明确跨树身份并验证集合行为。 |
| 表达式与资源格式参数 | 独立 `ParserExpression` 把起始 `{`、分隔 `:` 加入其结果；实际 `Parser.Parse` 使用另一条路径。`ResourceParser` 将 `CaptureCollection` 直接复制到 `string[]`。需分别补有效表达式和带参数资源的回归；现有用例只覆盖前者非法输入、后者无参数查找。 |

PluginSchemaTests 将产品 XSD 复制到夹具中，启用 `ReportValidationWarnings`，同时检测标准声明缺失和结构错误；适用的合法用例还实际执行加载与构建。动态成员名称通过 `lax` 保持可扩展，其缺失模式提示与结构错误分开验证。自定义模式可以使用 `BuiltinElementType` 声明构建器元素。已知后代结构错误仍须验证失败。这些测试不访问外部清单调查目录。

文档差异：`Plugin.GetParser` 的注释描述递归依赖查找，实际基础元素查询只查直接依赖、从插件和父插件；消费者不能仅凭传递依赖获得 Main 的基础注册。当前测试以运行时兼容行为为准，这处契约需在重构时统一。

未覆盖真实网络请求、交互终端、业务插件启动、程序集热替换/卸载、操作系统文件监视通知及所有异常回滚组合。Daemon 启停通过不代表上述路径已经验证。

---
name: zongsoft-core
description: 设计、修改、审查或测试 Zongsoft.Core 的公共契约与基础实现。适用于 Data、Messaging、Communication、Services、Configuration、IO、Security、Serialization 等跨项目 API，以及评估公共变更的兼容性和下游影响；不用于只属于某个驱动或第三方适配器的行为。
---

# Zongsoft.Core 公共契约

## 先确定契约所有者

1. 阅读 [AGENTS.md](AGENTS.md) 和目标领域现有接口、基类、扩展方法与测试。
2. 搜索 Data、Plugins、Web、messaging、externals 中的所有实现和消费者。
3. 只有多个生产实现具有相同语义时才修改 Core；单一实现需求留在下游。

## 兼容性检查

- 公共类型、成员、默认值、异常类型、取消和释放语义都属于兼容契约。
- 序列化、文本解析、配置键、URL/Topic、时间和数值格式变化需要兼容旧输入或明确迁移策略。
- 异步 API 不同步阻塞，不吞掉取消或后台异常；集合和注册表明确线程安全与快照语义。
- 可选能力通过 Feature、Provider、Factory 或现有扩展点表达，不用宽泛接口成员迫使所有实现伪支持。
- 热路径优化先确认分配、锁竞争和复杂度，再用聚焦测试或基准证明。

## 服务注册与解析调用链

- [ServiceCollectionExtension](src/Services/ServiceCollectionExtension.cs) 扫描程序集 ExportedTypes：优先执行 `IServiceRegistration`，否则处理 `ServiceAttribute`；通常将实现注册为 Singleton，契约指向同一实例，Members 分支读取静态成员。不要推断成 transient。
- 模块归属来自 [ApplicationModuleAttribute](src/Services/ApplicationModuleAttribute.cs)，可沿点分主程序集约定查找；[ApplicationModule.Services](src/Services/ApplicationModule.cs) 延迟建立命名容器，模块注册优先，再回退共享服务。模块容器不是每次 HTTP 请求的作用域。
- [ServiceProvider](src/Services/ServiceProvider.cs) 对集合结果先收集模块项，再补共享项，按具体类型去重；修改顺序与去重方式会影响 Find 和默认服务选择。
- [ServiceProviderExtension](src/Services/ServiceProviderExtension.cs) 区分别名 Resolve 与匹配器 Find；[ServiceLocator](src/Services/ServiceLocator.cs) 的 `name@provider` 先解析提供者，再调用 `IServiceProvider<T>.GetService(name)`，不是解析模块容器。
- [ServiceInjector](src/Services/ServiceInjector.cs) 处理可写公共字段/属性的服务和选项注入。`ServiceDependency.Provider` 选择模块容器，ServiceName 才是请求具名实例；`~`/`.` 可映射所属模块名。
- 应用自定义 `Module.Current` 及模块树节点由应用提供，Core 不存在通用的 `Module.Current` 单例。不要为了让示例编译新增这种全局 API。

## 配置与验证陷阱

- [XmlStreamConfigurationProvider](src/Configuration/Xml/XmlStreamConfigurationProvider.cs) 将 XML 属性映射为配置键，文本节点则按 `[value]` 集合项处理。写标量配置时核对实际键，不能套用其它 XML Provider 的直觉。
- 连接设置驱动、服务注册、提供者首次创建分属不同阶段。服务出现在容器中不证明配置或底层 SDK 可用。
- 服务测试路由：[ServiceLocatorTest](test/Services/ServiceLocatorTest.cs)、[TaggedServiceTest](test/Services/TaggedServiceTest.cs)。插件装配回归需另验证应用与模块容器、属性注入、配置键和共享实例身份。
- 所有权以实际注册与工厂为准，普通消费者不释放解析到的共享实例；实例被缓存、弱引用或静态复用也不等于自动实现取消/请求隔离。

## 领域路由

- 消息契约变化同时使用 [../messaging/SKILL.md](../messaging/SKILL.md) 检查全部驱动。
- 数据 Schema 或映射变化使用 [../Zongsoft.Data/SKILL.md](../Zongsoft.Data/SKILL.md)。
- 插件装配变化使用 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)。

## 验证

构建 `Zongsoft.Core.slnx` 并运行对应测试。公共行为变化至少选择一个真实下游实现做定向构建；多目标差异分别验证 net8.0、net9.0、net10.0。

## 字典变量视图

Variables.Wrap(IDictionary<string, object>) 承载字典实时视图，VariablesExtension.ToVariables 仅转发；已有 IVariables 直接返回，其它字典由私有嵌套适配器包装，ConditionalWeakTable 按引用身份复用且不阻止无外部引用对象回收。缓存同步不扩展到源字典。查询遵循 Variables 的完整键约定和 OrdinalIgnoreCase；标准 Dictionary、ConcurrentDictionary 使用相同比较器时直接查询，其它来源按枚举顺序返回第一个匹配项，不检查重名，null 值仍查询成功。测试覆盖引用身份、并发复用、GC 生命周期、实时修改和模板集成。详见 [表达式文档](docs/expressions.zh-Hans.md#字典变量视图)。

## Profile 指令与读取

读取机制见 [实现文档](docs/profiles.zh-Hans.md#读取与导入)。ProfileReader 识别紧接注释符的 @name，名称与参数以空格或 Tab 分隔；Argument 去除两端空白，语义由具体指令解释。ProfileReader 的内部嵌套类 Session 固定注册表与选项快照、调度指令并管理活动链及文件通知；ProfileReader 仅解析单个来源。Directives 子命名空间中的 ImportDirective 负责路径、可选/严格缺失和深度选项，经上下文内部读取操作共享会话，不能调用 Profile.Load 重建根会话。未知指令可由回调接管。

ProfileOptions 提供 PreserveBlanks、只读 Directives 集合与 Loading/Loaded；集合提供 Processing/Processed 和指回所属 ProfileOptions 的只读 Options。集合仅由所有者构造，克隆时必须绑定到新所有者。ProfileDirectiveOptions 按只读 Name 对应指令，Behavior 使用 None/Strict/Ignore/Suppress。集合名称忽略大小写，拒绝 null 和重名项，缺省配置采用指令内置默认值。ProfileDirectiveOptions.Import 创建公开嵌套 ImportOptions，MaximumDepth=0 采用默认 64，正数指定上限，负数拒绝；根配置计一层。根读取复制集合并调用每个选项的虚拟 Clone，含可变引用成员的派生类型负责复制这些成员。

Loading/Loaded 均为 Action<ProfileContext>，覆盖根与导入；文件打开及循环、深度检查后触发 Loading，解析和递归导入成功、子文件合并后触发 Loaded。FilePath/Depth/Referer/Profile 只读；匿名路径为空字符串，根 Referer 为 null，前置 Profile 为 null，后置为解析结果，前后使用不同上下文。缺失或被拒绝文件不通知，回调异常终止加载并清理，不回滚已有合并。

Directives.Processing/Directives.Processed 均为 Action<ProfileDirectiveContext>，每条指令一组，成功处理全部目标及递归导入后完成。前置可改 Argument 或设 Handled=true 接管；后置修改不重执行。上下文提供 Name、Argument、Handled、Behavior、Options、Profile、Section、FilePath、LineNumber、Depth。Options 是独立副本，指令实现直接读取它；前置回调可调整本条指令的专用设置（例如导入的 MaximumDepth），不影响会话设置、其它指令或已固定的 Behavior，未显式配置时为普通选项。Ignore 不执行且不通知指令回调；Suppress 在指令回调前拒绝；未知指令 None 未接管时保留注释，Strict 未接管则失败。文件通知与指令通知按递归顺序嵌套，保存保留原始指令注释。Profile 不提供 Variables 或自动变量展开。

[ApplicationManifest](docs/application-manifest.zh-Hans.md) 通过 Directives = { ProfileDirectiveOptions.Import(ProfileDirectiveBehavior.Suppress) } 禁止导入，并转换为 FormatException。Load(Stream) 关闭输入流，Load(TextReader) 保持读取器打开；基于 FileStream 的输入提供路径。Writer 只固定 PreserveBlanks，不复制指令选项、不执行指令或回调。Save() 按来源写回自身及导入子树的修改，显式输出仅处理当前配置；声明及保存规则见 [文档](docs/profiles.zh-Hans.md#声明与保存)。不为测试新增生产入口。

Profile.Directives 为全局线程安全注册表，内部使用 SynchronizedDictionary，默认 Directives.ImportDirective.Instance；Add 拒绝空值和重名，不提供移除或替换，枚举使用快照。每次根加载在克隆选项前复制注册表，递归共享实例引用；后续注册只影响下次加载，执行指令不持锁。ProfileDirectiveBase 提供 Name/Process，实现不得保存调用状态。选项集合不注册实现，Writer 不依赖任何指令实现。

ProfileExtension.ToVariables 为 Profile、ProfileSection、ProfileEntry 提供 IVariables 实时视图。配置读取当前有效集合；章节包含子树并保留完整命名空间；条目仅提供自身，章节和条目视图不随原集合替换而改换对象。章节层级用点连接，章节名称内的点保留，每个点分段须为 ASCII 标识符；条目名称中的点和连字符改为下划线后验证标识符。非法来源不提供变量，查询参数不裁剪或归一化。null/empty 命名空间等价默认命名空间，大小写忽略且不回退，值为 null 仍表示找到。仅查询到映射重名的变量时抛 ProfileException，不预先拒绝整个视图。适配器返回原始值，不求值、不缓存结果或复制字典，也不负责并发同步；模板通过显式注册视图求值，Profile 加载与保存仍保留原文。

## 本地搜索

Searcher 仅增强 System.IO.DirectoryInfo，详见 [机制文档](docs/searcher.zh-Hans.md)。Path 用于逻辑命名，Result 为实际目标，大小写按平台固定。搜索不穿过模式中的目录链接，显式起点可为链接；配置逻辑来源规则不变。不要将遍历提前移动到固定前缀，否则会绕过中间目录链接。

Searcher 统一通过 Search 扩展方法搜索；Searcher.Target.Files、Directories、Both 指定结果类型，默认 Both；Both = 0，调用方负责传入有效的 target，Search 不进行枚举值校验。

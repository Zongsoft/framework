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

## Profile 导入

导入机制见 [实现文档](docs/profiles.zh-Hans.md#读取与导入)。#@import 是 ProfileReader 内置语法，没有通用指令接口、注册集合、公开读写操作上下文或执行回调。Reader 每次根加载独立创建，递归共享，私有 Context 仅存 Profile、行号和章节；Profile 内部 Import 方法合并有效引用并登记来源关系。

ProfileOptions(bool preserveBlanks = true) 提供 PreserveBlanks、MaximumDepth 和两个可写的 Action<ProfileContext> 回调 Importing/Imported。Reader 浅复制空行选项、深度限制和委托引用，递归共享快照。ProfileContext 内部构造、公开 sealed，FilePath/Depth/Referer/Profile 只读；Referer 是直接引用者，根层数为 1；前置 Profile 为 null，后置为完成解析与合并的子文件，前后上下文分别创建。Reader 按 MaximumDepth（默认 64，仅接受正数，根文件计一层）限制导入深度并检测循环，没有禁用或跳过导入的选项；回调异常终止整个加载并清理，不回滚。根文件、可选缺失及内部拒绝不通知；回调捕获状态的线程安全由调用方保证。

Profile.Load 仅转发 Reader，Reader.ReadCore 管理流与活动状态，Parse 识别行及导入；独立根加载隔离，失败清理后可重试。Profile.Blanks 仅内部可写。保存由 ProfileWriter 承担，声明与有效引用分开，合并不修改被覆盖声明。Writer 不执行导入、不通知写入事件；导入文本是普通注释声明，编辑后需重新加载才能更新导入关系。

Save() 仅写回自身及导入子树中的修改来源，显式输出仅处理当前 Profile，回调配置不改变保存范围。所有临时输出准备成功后按导入后序提交；模型修改和重入仍拒绝，不增加测试专用生产扩展点。读写机制见 [声明与保存](docs/profiles.zh-Hans.md#声明与保存)，当前重构及平台结果见 [任务清单](PROFILE-BUILTIN-IMPORT-TASKS.md)。

## 本地搜索

Searcher 仅增强 System.IO.DirectoryInfo，详见 [机制文档](docs/searcher.zh-Hans.md)。Path 用于逻辑命名，Result 为实际目标，大小写按平台固定。搜索不穿过模式中的目录链接，显式起点可为链接；配置逻辑来源规则不变。不要将遍历提前移动到固定前缀，否则会绕过中间目录链接。

Searcher 统一通过 Search 扩展方法搜索；Searcher.Target.Files、Directories、Both 指定结果类型，默认 Both；Both = 0，调用方负责传入有效的 target，Search 不进行枚举值校验。

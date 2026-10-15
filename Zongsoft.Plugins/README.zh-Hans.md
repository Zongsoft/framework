# Zongsoft.Plugins 插件框架

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Plugins)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)


[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**P**lugins](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Plugins) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的应用装配层。轻量宿主通过声明式插件清单加载程序集、构建扩展树、注册服务并启动应用模块。业务类库可以不依赖插件运行时，只有宿主与装配项目需要了解插件加载。

该框架可用于终端程序、后台服务、Web 宿主和富客户端，适合需要独立打包、启用、替换或部署功能模块的系统。

## 核心概念

### 插件清单

`*.plugin` 是 XML 格式的装配契约，用于标识插件、声明程序集与插件依赖，并向扩展路径贡献对象。完整格式由 [`Zongsoft.Plugins.xsd`](Zongsoft.Plugins.xsd) 描述。

### 插件树

扩展项按可寻址路径组织成树。框架服务通常挂载在 `/Workbench` 下；例如主清单会暴露应用模块、服务、文件系统注册表、事件、命令执行器、连接设置驱动和诊断日志器。

### 构建器与解析器

构建器创建 `object`、`lazy`、`expose` 等节点；解析器负责解析 `path`、`type`、`static`、`option`、`service`、`resource` 等值。值表达式会在节点构建时求值，因此依赖顺序和当前路径上下文非常重要。属性装配先解析表达式或引用节点，再按目标成员类型及其转换器转换；string 和 object 属性也遵循此规则，不能将 `{option:...}` 等表达式当作普通文本直接赋值。求值结果可能在成员类型和转换器确定前就已缓存，因此赋值时仍尝试类型转换；保持 null 原值，避免将转换失败的结果变成值类型默认值。

### 应用上下文

`PluginApplicationContext` 表示正在运行的应用，并提供环境、模块、服务、事件、工作器和当前身份。不同宿主包提供具体上下文，业务模块则通过 Core 中的应用抽象交互。

## 安装

```shell
dotnet add package Zongsoft.Plugins
```

可部署宿主还需要主插件清单。NuGet 包已经包含标准部署附属文件，应将部署后的 `*.plugin` 放入应用的 `plugins` 目录。

## 创建宿主

宿主辅助方法将插件初始化接入 [.NET 通用宿主](https://learn.microsoft.com/zh-cn/dotnet/core/extensions/generic-host)：

```csharp
using Microsoft.Extensions.Hosting;
using Zongsoft.Plugins.Hosting;

await Application.Terminal(args).RunAsync();
```

后台服务使用 `Application.Daemon(args)`；Web 应用使用配套的 [Zongsoft.Plugins.Web](../Zongsoft.Plugins.Web/README.zh-Hans.md) 包。

构建器会加载应用配置、发现插件目录、构建插件树、注册模块与服务、初始化应用上下文，最后启动宿主。

## 声明插件

以真实的 [Discussions 业务清单](../../discussions/src/Zongsoft.Discussions.plugin)为例。下面摘录程序集、依赖和模块节点；它不是完整清单，部署时保留原文件中的验证器、数据过滤器和身份扩展：

```xml
<manifest>
	<assemblies>
		<assembly name="Zongsoft.Discussions" />
	</assemblies>
	<dependencies>
		<dependency name="Zongsoft.Data" />
		<dependency name="Zongsoft.Security" />
	</dependencies>
</manifest>

<extension path="/Workbench/Modules">
	<object name="Discussions" value="{static:Zongsoft.Discussions.Module.Current, Zongsoft.Discussions}">
		<expose name="Accessor" value="{path:../@Accessor}">
			<expose name="Filters" value="{path:../@Filters}" />
		</expose>
		<expose name="Events" value="{path:../@Events}" />
		<expose name="Properties" value="{path:../@Properties}" />
	</object>
</extension>
```

插件名为 `Zongsoft.Discussions`，模块名为 `Discussions`；两者不是可以互换的名称。程序集定义见[项目文件](../../discussions/src/Zongsoft.Discussions.csproj)，模块对象见 [Module.cs](../../discussions/src/Module.cs)。业务库引用 Core 的公共契约，数据引擎与安全实现通过运行时插件依赖提供。

🚨 不要只复制这个摘录替代完整清单，否则会漏掉站点约束、帖子过滤和身份转换。编译成功也不能证明依赖插件、程序集和配置已正确部署。

## 插件化部署：从宿主到运行能力

### 三个相互独立的环节

| 环节 | 负责什么 | 不负责什么 |
| --- | --- | --- |
| NuGet/项目引用 | 让宿主或业务类库能够针对 API 编译 | 不会装配宿主的插件目录 |
| 部署 | 将清单、程序集、依赖、选项、映射及资源复制到运行位置 | 不会启动应用或初始化数据库 |
| 插件加载 | 在宿主中读取清单、解析依赖、构造节点并注册服务 | 不会下载缺失的包 |

宿主是启动器，不是业务模块。[Zongsoft/hosting](https://github.com/Zongsoft/hosting) 提供现成的终端、后台及 Web 启动器，其部署清单独立于宿主源码组合各项能力。

### 真实项目的部署组成

[Main.plugin](plugins/Main.plugin) 和 [Terminal.plugin](plugins/Terminal.plugin) 是框架现有的基础清单。论坛业务模块则由 Discussions 的[领域部署清单](../../discussions/src/Zongsoft.Discussions.deploy)和 [Web 部署清单](../../discussions/src/api/Zongsoft.Discussions.Web.deploy)交付。

在兼容的已有测试宿主部署方案中加入以下组合片段；它只表示两个业务包的部署目标，不代替宿主完整清单：

```ini
[plugins zongsoft discussions]
nuget:Zongsoft.Discussions

[plugins zongsoft discussions web]
nuget:Zongsoft.Discussions.Web
```

领域包实际包含 `Zongsoft.Discussions.dll`、同名 `.plugin`、`.option`、`.mapping`；Web 包包含 `Zongsoft.Discussions.Web.dll`、同名 `.plugin` 和归档模板。包根 `.deploy` 从 `artifacts/` 与 `lib/${Framework}/` 复制这些文件，不应把包内路径当作源码目录路径。

### 加入业务插件

宿主负责启动；Discussions 负责论坛领域；Web 包负责 HTTP 适配。部署时还需显式组合 Data、Security、数据库驱动及所选文件存储提供者，不能只增加两行业务包就认为所有依赖齐备。连接、站点与文件路径的前置条件见 [Data 真实业务用例](../Zongsoft.Data/README.zh-Hans.md#plugin-quickstart)。

插件的文件系统层级与 `/Workbench/Modules` 等逻辑扩展路径不同。保留部署方案的目录布局和清单中的依赖关系；启动后检查插件树与 `Discussions` 模块，再验证实际接口，不要改写宿主入口来硬编码业务依赖。

💡 本地验证使用独立部署目录、测试数据库及测试身份。具体宿主启动和部署命令见[已有宿主说明](https://github.com/Zongsoft/hosting)与[部署工具](https://github.com/Zongsoft/tools/tree/main/deployer)；这里不另造一个不存在于仓库中的演示宿主项目。

### 包部署清单与源码构建

包根目录的 `.deploy` 通常引用包内 `artifacts/` 和 `lib/${Framework}/`。应通过 `nuget:Package` 条目执行；把这些文本复制到源码目录，不会让包内路径自动存在。

本地调试时先停止测试宿主，构建匹配的配置/TFM，将变化的程序集及配套清单、选项、映射、资源复制到既有插件位置，再重启。也可在应用自己的 `.deploy` 中显式引用源码构建产物。不要只为替换一个调试 DLL 就执行全量部署，它可能覆盖本地配置或移除手工安装的文件。

未指定包内路径时，部署工具优先处理包根 `.deploy`，选择适配 TFM 的资源并处理嵌套条目。默认依赖下载器跳过 `System.*`、`Microsoft.Extensions.*` 和 `Zongsoft.*`，所以必须显式组合需要的 Zongsoft 插件，并保证宿主运行依赖兼容。

### 配置也是装配的一部分

插件配置提供程序在清单旁只选择一个入口：先将插件文件扩展名 `.plugin` 改为 `.option` 查找；只有该文件不存在，才查找 `<plugin name="…">.option`。例如 `Feature-daemon.plugin` 的插件名为 `Feature.Daemon`，优先选择 `Feature-daemon.option`，不存在时才使用 `Feature.Daemon.option`。两者都不存在则不加载配置。第一个入口只要存在，就不会再加载第二个；空文件或无效 XML 不触发回退，解析错误直接报错。

提供程序不扫描后缀、环境名、宿主名、站点名或子目录。配置片段仅通过 `<?import path="…" required="false"?>` 显式导入；路径相对于声明文件，末尾导入覆盖前面的同名键。可选导入只允许文件缺失，不忽略解析错误。宿主 `LoadConfiguration("web")` 只加载 `web.option`，尊重配置文件提供程序及其根目录；需要 `web-default.option` 时应显式导入。新增或删除插件入口后，调用提供程序的 `Load()` 或重启以重新选择入口；现有入口及导入文件的变化遵循 XML 提供程序的变更通知机制。准确规则见[提供程序源码](src/Configuration/PluginConfigurationProvider.cs)。

首次加载和显式 `Load()` 均包含所有已加载的子插件。加载、卸载、整体重载及已关联文件的重载会通知宿主配置，并为下一次变更提供新令牌；替换、卸载或释放提供程序会解除旧文件订阅。整体重载失败会清理已建立的提供程序，调用方应处理异常后重试。整体重载、插件树加载/卸载与释放应串行调用；通知不代表多个文件的原子更新，不自动新增监听或热替换程序集。

服务扫描发生在整棵插件树和选项加载完成之后。已加载的入口引用、入口程序集与插件清单共用去重集合，每个程序集在一次宿主构建中只注册一次；`IServiceRegistration.Register` 可以读取子插件配置。这让协议扩展能够先注册，再由宿主建立网络端点快照。

部署变量和运行时选项不同。传给部署器的 `--site:daemon` 可以选中 `*-daemon.plugin` 产物；传给宿主的 `site=daemon` 保留为应用上下文配置，不参与选项文件筛选。一些插件依赖附加清单启动工作器，仅安装主 DLL 不会启用这些工作器。

不要依赖多个插件定义同一配置键时的偶然顺序。环境/站点设置应有明确所有者，验证最终生效值时不得打印秘密。

诊断冲突时，通过 `PluginConfigurationProvider.Providers` 分别读取各插件的生效值；每项是一个 `CompositeConfigurationProvider`，包含处理所选入口及其导入的单个 XML 提供程序。对于 `FileConfigurationProvider` 实例 `file`，由 `file.Source.FileProvider.GetFileInfo(file.Source.Path).PhysicalPath` 取得配置入口的本地路径；该来源不追踪单个键来自哪个导入文件。先在同插件内完成导入覆盖，再比较插件间的值，不能把正常覆盖报为冲突。Plugins 保留通用入口信息；网络键的冲突规则由 Web 宿主消费层负责，不能仅凭最终合并值判断。

`<?binding ...?>` 是供交付工具消费的暴露声明，Plugins 不执行其中的模板、不生成代理配置，也不因此启动监听。

### 验证与操作风险

1. 确认目标宿主内容根目录中存在 `plugins/Main.plugin` 和各功能清单。
2. 核对插件名、依赖、程序集版本、TFM 及原生运行时架构。
3. 检查 `plugin.list`/`plugin.tree`，再验证插件贡献的具名服务、驱动、命令或 Web 端点。
4. 验证生效配置，最后才连接已授权的测试服务。

🚨 部署会复制并可能覆盖文件，清单还可能包含删除条目。替换前应备份本地配置、确认解析后的目标并停止宿主。插件以宿主权限运行，不是安全沙箱；配置文件支持变化加载，不代表 DLL 可以安全热替换。

更大的多插件应用可参考[宿主部署范例](https://github.com/Zongsoft/hosting)与[部署器语法和选项](https://github.com/Zongsoft/tools/tree/main/deployer)。

## 真实消费插件：Discussions 的模块与服务

### 模块容器定位公共契约

[Module.cs](../../discussions/src/Module.cs) 在程序集上声明 `ApplicationModule(Module.NAME)`，并定义模块单例。以下是类内部的实际成员摘录：

```csharp
public const string NAME = nameof(Discussions);
public static readonly Module Current = new();

public Module() : base(NAME) { }

private IDataAccess _accessor;
public IDataAccess Accessor => _accessor ??=
	this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
```

模块通过自己的 `Services` 解析 Core 的 `IDataAccessProvider`，按模块名取得访问器；没有直接构建数据库驱动。完整模块还定义事件注册表，不能把这个成员摘录当作完整类文件。服务容器的模块优先、共享回退与所有权规则见 [Core](../Zongsoft.Core/README.zh-Hans.md)。

### 清单、配置与数据映射各司其职

- [业务清单](../../discussions/src/Zongsoft.Discussions.plugin)将模块、验证器、过滤器和身份扩展装配到插件树。
- [业务选项](../../discussions/src/Zongsoft.Discussions.option)在 `/Discussions` 下提供 `general` 的 `siteId`、`basePath`；XML 属性形成标量配置键。仓库中的站点值和存储路径不是生产配置。
- [数据映射](../../discussions/src/Zongsoft.Discussions.mapping)定义实际论坛实体及关系；[数据库脚本](../../discussions/database/)负责对应数据库的结构。
- [ForumService](../../discussions/src/Services/ForumService.cs)通过 `[Service]` 注册并使用 Core 的数据服务基类；[ForumController](../../discussions/src/api/Controllers/ForumController.cs)消费该领域服务。

### 从源码追踪一次调用

以获取版主为例：控制器读取请求中的数据模式，调用 `ForumService.GetModerators`；服务使用 `IDataAccess.Select<UserProfile>` 查询 `ForumUser` 关系。数据引擎、连接与驱动由宿主部署和配置提供，业务对象不持有具体数据库客户端。安全和站点处理需要完整插件链路，不能把单个查询片段当成独立的授权边界。

### 验证与所有权

核对现有 [.http 请求](../../discussions/docs/http/forum.http)中的路径模板，用实际测试站点和论坛标识替换参数；不要复制其中的身份或服务地址。验证插件加载、容器解析、配置、映射、数据库和权限后再判断业务结果。本节引用当前源码，不声称一次历史隔离探针已验证 Discussions 的完整业务链路。

Windows 终端自动化需要有效控制台句柄；从部署目录启动并保留宿主 `runtimes/` 等依赖。共享容器返回的服务不由单次消费代码释放。更新程序集后重启隔离宿主，退出后仅清理自己创建的测试资源。

## 加载与生命周期

1. 发现插件清单并解析插件依赖。
2. 加载声明的程序集并注册应用服务。
3. 在插件树需要时构建扩展节点。
4. 初始化应用上下文和模块。
5. 启动工作器和底层宿主。
6. 宿主关闭时停止工作器并释放已构建组件。

构建失败信息会包含插件路径和组件上下文。构造函数中应避免副作用；可启动工作应放入应用模块或工作器，以保证关闭和失败清理具有确定性。

## 配置职责

`PluginConfigurationSource` 将插件选项接入 Microsoft 配置体系。不同文件的职责应严格区分：

- `*.plugin` 装配插件与扩展节点。
- `*.option` 提供应用选项。
- `*.mapping` 描述 Data 实体和命令。
- `*.deploy` 控制包部署。

> 💡 标准工作台树可参考 [`plugins/Main.plugin`](plugins/Main.plugin)，终端专用装配可参考 [`plugins/Terminal.plugin`](plugins/Terminal.plugin)。

## 排障

- 使用插件 `Tree`、`List` 和 `Find` 命令查看最终扩展树。
- 优先处理第一条缺失依赖或程序集错误，不要只关注后续组件失败。
- 路径具有上下文，相关路径表达式从当前节点开始计算。
- 确认配置与部署附属文件已经复制到插件清单附近。

## 相关资源

- [插件 XML 模式](Zongsoft.Plugins.xsd)
- [主插件示例](plugins/Main.plugin)
- [终端插件示例](plugins/Terminal.plugin)
- [Web 插件宿主](../Zongsoft.Plugins.Web/README.zh-Hans.md)
- [.NET 通用宿主](https://learn.microsoft.com/zh-cn/dotnet/core/extensions/generic-host)

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

构建器创建 `object`、`lazy`、`expose` 等节点；解析器负责解析 `path`、`type`、`static`、`option`、`service`、`resource` 等值。值表达式会在节点构建时求值，因此依赖顺序和当前路径上下文非常重要。

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

```xml
<?xml version="1.0" encoding="utf-8" ?>
<plugin name="Acme.Inventory" title="Inventory Module">
	<manifest>
		<dependencies>
			<dependency name="Main" />
		</dependencies>
		<assemblies>
			<assembly name="Acme.Inventory" />
		</assemblies>
	</manifest>

	<extension path="/Workbench/Modules">
		<object name="Inventory" type="Acme.Inventory.Module, Acme.Inventory" />
	</extension>
</plugin>
```

依赖项使用的 `name` 必须与目标插件名称完全一致；程序集文件必须位于已部署的插件位置，或能够被宿主解析。

> 🚨 项目编译成功并不代表插件一定能加载，因为清单是运行时契约。插件名、依赖名、程序集名、扩展路径和部署文件必须保持同步。

## 插件化部署：从宿主到运行能力

### 三个相互独立的环节

| 环节 | 负责什么 | 不负责什么 |
| --- | --- | --- |
| NuGet/项目引用 | 让宿主或业务类库能够针对 API 编译 | 不会装配宿主的插件目录 |
| 部署 | 将清单、程序集、依赖、选项、映射及资源复制到运行位置 | 不会启动应用或初始化数据库 |
| 插件加载 | 在宿主中读取清单、解析依赖、构造节点并注册服务 | 不会下载缺失的包 |

宿主是启动器，不是业务模块。[Zongsoft/hosting](https://github.com/Zongsoft/hosting) 提供现成的终端、后台及 Web 启动器，其部署清单独立于宿主源码组合各项能力。

### 完整的本地终端示例

准备临时工作目录和 .NET 10 SDK。以下是供读者执行的步骤，不应直接用于现有应用目录：

```shell
dotnet new console -n PluginDemo -f net10.0
cd PluginDemo
dotnet add package Zongsoft.Plugins
```

将 Program.cs 替换为以下完整入口：

```csharp
using Microsoft.Extensions.Hosting;
using Zongsoft.Plugins.Hosting;

Application.Terminal("PluginDemo", args).Run();
```

在项目目录创建以下 `.deploy`。以空格分隔的章节表示目标路径：

```ini
[plugins]
nuget:Zongsoft.Plugins/plugins/Main.plugin
nuget:Zongsoft.Plugins/plugins/Terminal.plugin

[plugins zongsoft commands]
nuget:Zongsoft.Commands
```

按需安装[部署工具](https://github.com/Zongsoft/tools/tree/main/deployer)，发布启动器，再部署功能插件：

```shell
dotnet tool install -g Zongsoft.Tools.Deployer
dotnet publish -c Release -f net10.0 -o out
dotnet deploy --destination:./out --framework:net10.0 --edition:Release --platform:win --architecture:x64
cd out
dotnet PluginDemo.dll
```

上述平台/架构表示 Windows x64 示例，含原生依赖的插件应使用真实目标。选择兼容包版本，并在可复现部署中用 `package@version` 固定版本。已安装工具时不必重复安装。

进入交互提示符后执行 `help`、`echo hello`、`plugin.list`。宿主项目并未引用 Zongsoft.Commands，该能力通过部署及清单加入。用 `exit` 停止程序。

相关产物结构如下：

```text
out/
	PluginDemo.dll
	PluginDemo.deps.json
	PluginDemo.runtimeconfig.json
	plugins/
		Main.plugin
		Terminal.plugin
		zongsoft/
			commands/
				Zongsoft.Commands.plugin
				Zongsoft.Commands.dll
```

图中省略了其它宿主依赖与附属资源，不代表部署时可以省略。

### 加入业务插件

业务代码放在独立类库中，清单声明程序集、依赖及扩展贡献；前面的 Inventory 清单展示这种结构，其中 Inventory.Module 类型需要由应用定义。把类库和清单部署到 `plugins` 下，并按需配套选项、映射，宿主 Program.cs 无需改变。

文件系统层级决定插件父子关系，而 `/Workbench/Modules` 等扩展路径是另一套逻辑层级，不要随意扁平化既有部署。依赖名称在已加载树中查找，比较时忽略大小写；仍应保留规范拼写并避免重名。

### 包部署清单与源码构建

包根目录的 `.deploy` 通常引用包内 `artifacts/` 和 `lib/$(Framework)/`。应通过 `nuget:Package` 条目执行；把这些文本复制到源码目录，不会让包内路径自动存在。

本地调试时先停止测试宿主，构建匹配的配置/TFM，将变化的程序集及配套清单、选项、映射、资源复制到既有插件位置，再重启。也可在应用自己的 `.deploy` 中显式引用源码构建产物。不要只为替换一个调试 DLL 就执行全量部署，它可能覆盖本地配置或移除手工安装的文件。

未指定包内路径时，部署工具优先处理包根 `.deploy`，选择适配 TFM 的资源并处理嵌套条目。默认依赖下载器跳过 `System.*`、`Microsoft.Extensions.*` 和 `Zongsoft.*`，所以必须显式组合需要的 Zongsoft 插件，并保证宿主运行依赖兼容。

### 配置也是装配的一部分

插件配置提供程序加载清单旁同主文件名的选项：基础 `Feature.option`、环境 `Feature.development.option`，再加载匹配的环境后缀和 host/site 专属文件。宿主级 `web.option` 或应用名称选项单独加载。准确匹配规则见[提供程序源码](src/Configuration/PluginConfigurationProvider.cs)，不能假设任意 `.option` 都会被扫描。

部署变量和运行时选项不同。传给部署器的 `--site:daemon` 可以选中 `*-daemon.plugin` 产物；传给宿主的 `site=daemon` 参与运行时配置选择。一些插件依赖附加清单启动工作器，仅安装主 DLL 不会启用这些工作器。

不要依赖多个插件定义同一配置键时的偶然顺序。环境/站点设置应有明确所有者，验证最终生效值时不得打印秘密。

### 验证与操作风险

1. 确认目标宿主内容根目录中存在 `plugins/Main.plugin` 和各功能清单。
2. 核对插件名、依赖、程序集版本、TFM 及原生运行时架构。
3. 检查 `plugin.list`/`plugin.tree`，再验证插件贡献的具名服务、驱动、命令或 Web 端点。
4. 验证生效配置，最后才连接已授权的测试服务。

🚨 部署会复制并可能覆盖文件，清单还可能包含删除条目。替换前应备份本地配置、确认解析后的目标并停止宿主。插件以宿主权限运行，不是安全沙箱；配置文件支持变化加载，不代表 DLL 可以安全热替换。

更大的多插件应用可参考[宿主部署范例](https://github.com/Zongsoft/hosting)与[部署器语法和选项](https://github.com/Zongsoft/tools/tree/main/deployer)。

## 完整消费插件：只依赖公共接口

这里在前面的 PluginDemo 终端宿主中增加一个计算命令。消费类库只引用 Core，语言提供者由部署方案选择；宿主入口不改动。

### 1. 编写应用模块

在 PluginDemo 的同级目录创建 `Acme.Rules` 类库：

```shell
dotnet new classlib -n Acme.Rules -f net10.0
cd Acme.Rules
dotnet add package Zongsoft.Core
```

选择与宿主兼容的 Core 版本。将以下完整代码保存为 `EvaluateCommand.cs`，再执行 `dotnet build -c Debug`：

```csharp
using Zongsoft.Components;
using Zongsoft.Expressions;
using Zongsoft.Services;

[assembly: ApplicationModule("Rules")]

namespace Acme.Rules;

public sealed class Module : ApplicationModule
{
	public static readonly Module Current = new();
	private Module() : base("Rules") { }
}

public sealed class EvaluateCommand : CommandBase<CommandContext>
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var name = ApplicationContext.Current.Configuration["Rules:Evaluator"]
			?? throw new InvalidOperationException("Rules:Evaluator is missing.");
		var evaluator = Module.Current.Services.FindRequired<IExpressionEvaluator>(name);
		var result = evaluator.Evaluate("x + y", new Dictionary<string, object>
		{
			["x"] = 20,
			["y"] = 22,
		});
		context.Output.WriteLine(result);
		return ValueTask.FromResult(result);
	}
}
```

`Module.Current` 是此应用定义的模块入口。程序集上的模块注解决定服务归属，清单中的模块节点把该模块加入当前应用。模块容器优先查找自己的注册，找不到时回退应用共享服务；并不要求把每个提供者重新注册到各个模块。

### 2. 声明清单与选项

类库目录中的 `Acme.Rules.plugin`：

```xml
<?xml version="1.0" encoding="utf-8"?>
<plugin name="Acme.Rules">
	<manifest>
		<assemblies>
			<assembly name="Acme.Rules" />
		</assemblies>
		<dependencies>
			<dependency name="Main" />
		</dependencies>
	</manifest>
	<extension path="/Workbench/Modules">
		<object name="Rules" value="{static:Acme.Rules.Module.Current, Acme.Rules}" />
	</extension>
	<extension path="/Workbench/Executor/Commands">
		<object name="Evaluate" type="Acme.Rules.EvaluateCommand, Acme.Rules" />
	</extension>
</plugin>
```

同目录 `Acme.Rules.option`：

```xml
<options>
	<option path="/">
		<rules evaluator="Scriban" />
	</option>
</options>
```

`rules` 的 `evaluator` **属性**生成 `Rules:Evaluator` 键。XML 文本节点在此配置提供程序中表示集合项，不能把 `<evaluator>Scriban</evaluator>` 当成同一标量键。

### 3. 部署并调用

在 PluginDemo 现有 `.deploy` 末尾补充：

```ini
[plugins zongsoft externals scriban]
nuget:Zongsoft.Externals.Scriban

[plugins acme rules]
../Acme.Rules/bin/Debug/net10.0/Acme.Rules.dll
../Acme.Rules/Acme.Rules.plugin
../Acme.Rules/Acme.Rules.option
```

停止宿主后，从 PluginDemo 项目目录再次执行前文的部署命令，然后在 `out` 目录启动宿主并输入 `evaluate`，预期输出 `42`。部署后目录包含独立的 Scriban 实现插件及 Acme.Rules 消费插件；消费代码不需要 `using Zongsoft.Externals.Scriban`。

本例使用纯本地算术，不需要数据库或云服务。结束后用 `exit -yes` 关闭；可删除自己创建的示例部署目录，不要清理现有业务宿主。

### 4. 注入、选择与所有权

如果业务对象拥有 `IExpressionEvaluator Evaluator { get; set; }` 公共属性，装配层也可以用 `Evaluator="{service:Scriban@}"` 注入应用容器中的具名求值器；`{service:@Rules}` 返回 Rules 模块容器本身。XML 负责选择和装配，业务代码仍面向接口。

配置切换只有在双方实现同一契约**且满足应用语义**时成立：Lua 使用自己的表达式语法，不能仅改名称就认为所有 Scriban 脚本可执行。求值器是共享注册，不对解析结果逐次 `Dispose()`。缓存/序号等需要具名实例的场景，使用提供者的 `GetService(name)` 或 `Locate<T>("name@provider")`，详见 [Core 服务定位](../Zongsoft.Core/README.zh-Hans.md)与[Redis 完整示例](../externals/redis/README.zh-Hans.md)。

### 本地验证记录与边界

.NET 10 Windows 隔离部署中，已使用 Zongsoft 与 Automao 的终端启动器验证：配置读取、按名称取得 Scriban、结果 `42`、模块回退共享实例、插件属性注入及具名 Redis 缓存读写/清理。验证消费插件只引用 Core 契约，没有更改宿主业务代码；这不代表所有插件或真实业务流程均已验证。

- Windows 终端宿主及当前部署工具需要有效控制台句柄；自动化运行应分配 PTY/ConPTY，不能假定普通管道与交互式控制台等价。
- 从部署目录启动，或显式配置内容根。DLL 绝对路径本身不改变工作目录。
- 手工复制宿主输出时保留 `runtimes/` 等依赖子目录；更新 Core 相关插件时核对宿主根目录的 Core 版本。
- 部署工具可能以退出码 0 结束但在输出中记录条目错误。检查错误信息、预期文件及首次接口调用。

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

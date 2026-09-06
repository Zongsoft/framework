# Zongsoft.Externals.Grapecity 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**G**rapecity](https://github.com/Zongsoft/framework/tree/main/externals/grapecity) 将 [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) 适配到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的报表抽象中。

## 主要功能

- 为 ActiveReports 解析 Zongsoft 报表描述、数据源、参数和数据模型；
- 从框架的报表定位器及数据模型提供程序加载报表定义；
- 提供基于 ActiveReports 的报表设计器资源和主题集成；
- 可配合配套的 [Web 程序包](api)托管报表查看器与设计器端点。

请在 `Zongsoft.Reporting` 之后加载 `Zongsoft.Externals.Grapecity.plugin`。ActiveReports 是商业授权软件，请确保目标环境具备所需的运行时和设计器许可证。

## 安装与打开定义

```shell
dotnet add package Zongsoft.Externals.Grapecity
```

真实参考可从适配器的 [Report.Open(IReportDescriptor)](src/Reporting/Report.cs) 实现入手：

```csharp
public static Report Open(IReportDescriptor descriptor)
{
	if(descriptor == null)
		throw new ArgumentNullException(nameof(descriptor));

	using var stream = descriptor.Open();
	return stream == null ? null : Open(stream);
}
```

这段源码展示框架描述符与厂商引擎之间的边界，并不是要求使用者手动构造应用服务。仓库未附带销售报表 RDLX 模板，也没有预定义的 `Title` 参数。请部署可信的 ActiveReports 定义，并只使用其中实际声明的参数。宿主中的查看器和设计器组合方式见 [Web 包](api/README.zh-Hans.md)。

`Report.Open` 接受框架文件路径、数据流或 `IReportDescriptor`。它包装 ActiveReports `PageReport`，按第一个报表项分类为固定页（`FPL`）或连续页（`CPL`），并把 ActiveReports 参数/数据源投射到 Zongsoft 报表契约。

## 数据与设计器资源

ActiveReports 数据源定义转换为 `ReportDataSource`/`ReportDataModel`；已注册 `IReportDataLoader` 必须把模型物化为运行数据。`ResourceService` 与 `ThemeResolver` 把报表定位器、图片、模板和主题接入 ActiveReports 设计器。

> 💡 不要在 RDLX 定义中保存凭据。由可信 `IReportDataLoader` 把逻辑数据源名称和参数化 Schema 映射到环境专属数据访问。

## 当前限制

当前适配器可以打开、检查、编辑和保存报表定义，并支持配套查看器/设计器中间件。但直接 `IReport.Render*` 会抛出 `NotImplementedException`，`Export*` 尚无实现，设置 `IReport.Locator` 也未实现。请使用已实现的 [Web 集成](api/README.zh-Hans.md)，并在采用前验证所需导出行为。

🚨 ActiveReports 模板可能包含表达式和数据查询。只加载可信定义，授权底层数据，限制资源与输出路径、报表大小和运行时长，并遵守 MESCIUS/GrapeCity 运行时及设计器许可。

## 延伸阅读

- [Zongsoft.Reporting 契约](../../Zongsoft.Reporting/README.zh-Hans.md)
- [ActiveReports 文档](https://developer.mescius.com/activereportsnet/docs/introduction/getstarted)
- [Web 查看器/设计器集成](api/README.zh-Hans.md)
- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

还需部署 Reporting 及获得许可的 ActiveReports 运行时。适配器源码 `.deploy` 列出自身产物，但未列完整商业运行时，应单独核对所选包依赖及许可证。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Grapecity` | [Zongsoft.Externals.Grapecity.plugin](src/Zongsoft.Externals.Grapecity.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Grapecity.deploy](src/Zongsoft.Externals.Grapecity.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting

[plugins zongsoft externals grapecity]
nuget:Zongsoft.Externals.Grapecity
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Grapecity.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

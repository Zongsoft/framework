# Zongsoft.Reporting 报表插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Reporting)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Reporting)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**R**eporting](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Reporting) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的报表插件库，提供了报表开发的基础结构。

本包定义与厂商无关的契约，用于定位报表定义、解析厂商格式、加载数据、绑定参数、渲染、导出、存储资源和归档结果。它不包含完整报表设计器或渲染引擎；格式专属操作必须由提供程序适配器实现。

## 报表术语

| 概念 | 契约 | 用途 |
| --- | --- | --- |
| 定义 | `IReportDescriptor` | 标识模板并打开源数据流 |
| 报表 | `IReport` | 带参数、数据定位器、渲染和导出操作的提供程序文档 |
| 资源 | `IReportResource` | 报表使用的具名内嵌/外部资源 |
| 数据模型 | `IReportDataModel` | 描述数据源、Schema、分页和提供程序设置 |
| 定位器/加载器 | `IReportDataLocator`, `IReportDataLoader` | 选择加载器并异步取得报表数据 |
| 仓储/归档 | `IReportRepository`, `IReportArchiveLocator` | 持久化定义或定位生成结果 |

渲染产生报表的呈现格式，导出产生选定的交换/输出格式。准确选项和可用格式由提供程序决定。

## 安装

```shell
dotnet add package Zongsoft.Reporting
```

还需安装实现 `IReportResourceResolver` 与具体 `IReport` 的报表提供程序，并把它的定位器和仓储注册到应用服务容器。

## 定义示例

通过描述符异步打开模板，使用完后释放数据流：

```csharp
var descriptor = new FileReportDescriptor("templates/sales.rdlx");
await using var stream = await descriptor.OpenAsync(cancellation);
```

本包未附带可直接运行的报表定义。请准备所选提供程序支持的可信模板，并使用该提供程序的打开 API 获得 IReport；模板格式、数据源和参数均以实际定义为准。

描述符负责打开模板，IReportResourceResolver.ResolveAsync 负责解析图片等资源。渲染和导出只应调用所选提供程序已经实现的操作。

> 💡 业务查询应封装在 `IReportDataLoader` 后。模板应只命名数据模型与参数，不应嵌入部署凭据或不受限的临时数据访问。

## 异步加载数据

通过 `await loader.LoadAsync(report, model, cancellation)` 取得数据。返回对象可以是异步查询结果；报表提供程序需要异步读取数据，再向要求内存集合的渲染 API 提供已就绪的数据。

当前 GrapeCity 适配器与本版本的异步加载、定位和存储契约不兼容，不能组合使用。

模板和资源的定位、仓储创建/更新/删除、报表保存均使用 Async 入口并支持取消。IReportArchiveLocator.OpenAsync 返回 ReportArchiveResult：未找到时为默认值；没有文件路径时仍可返回有效的 Archive 元数据；非空 Stream 由调用方通过 await using 释放。

## 生命周期与限制

除非具体提供程序另有说明，调用者拥有自己打开或传入的数据流。渲染可能大量消耗 CPU、内存与 I/O；请限制输入大小、行数、分页、输出大小、字体/图片与执行时间，长耗时报表应交给后台任务。

🚨 当提供程序支持表达式或脚本时，应把报表模板视为主动且有权限的输入。只加载可信定义，限制文件/资源解析，参数化数据访问，清理导出文件名，并同时授权报表定义及其底层数据。

当前内置 `ReportDataLocator` 只是服务占位实现，不会选择加载器。应用必须提供可用的定位器/加载器或提供程序包；不能假设只安装本契约包就能启用数据报表。

## 延伸阅读

- [公开报表契约](src/)
- [Zongsoft.Data](../Zongsoft.Data/README.zh-Hans.md)
- [实现协作指南](SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

服务扫描注册内置数据加载器/定位器，仍需具体报表引擎与应用数据服务；当前定位器尚未完成，限制见前文。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Reporting` | [Zongsoft.Reporting.plugin](src/Zongsoft.Reporting.plugin) |
| 文件复制及依赖 | [Zongsoft.Reporting.deploy](src/Zongsoft.Reporting.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Reporting.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

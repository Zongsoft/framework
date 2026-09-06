# Zongsoft.Externals.Grapecity.Web 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Grapecity.Web` 在 Zongsoft Web 应用中托管 [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) 查看器和设计器集成。它将 ActiveReports HTTP 服务连接到 `Zongsoft.Reporting` 与 `Zongsoft.Externals.Grapecity` 提供的报表定位器、模板、数据源和数据模型。

## Web 端点

该程序包提供报表列表、查看器报表定义，以及设计器资源、模板、缩略图、主题和数据模型等端点。报表发现入口为 `GET /Grapecity/Reporting/Reports`，模板端点位于 `/Grapecity/Reporting/Templates` 下。

请在核心 GrapeCity 与报表插件之后加载 `Zongsoft.Externals.Grapecity.Web.plugin`，并在公开查看器或设计器端点前，由宿主应用配置身份认证和授权。

## 安装与启动

```shell
dotnet add package Zongsoft.Externals.Grapecity.Web
```

注册的 `IApplicationInitializer<IApplicationBuilder>` 使用共享 `/Grapecity/Reporting` 前缀、压缩、自定义报表存储和数据源回调调用 `UseReporting` 与 `UseDesigner`。数据报表需要已配置的 `IReportDataLoader`，报表定义由 `IReportLocator` 实现提供。

```http
GET /Grapecity/Reporting/Reports
Accept: application/json
```

该端点返回所有已注册定位器中的报表名称，无结果时返回 `204`。同一前缀下的 ActiveReports 查看器/设计器路由由厂商中间件拥有，请依据其客户端包与文档确定请求格式。

> 💡 加载顺序很重要：报表契约、GrapeCity 核心适配器、报表/数据提供程序，最后才是本 Web 插件。缺少报表通常是定位器或插件发现问题，而不是浏览器渲染问题。

## 安全与运维

🚨 设计器可读取定义/资源，并可能经存储保存修改后的模板。请分别授权查看器与设计器、禁止普通用户访问设计功能、验证报表键、限制上传/资源，并按宿主凭据类型应用防伪规则。

报表执行可能开销很大。请限制行数、输出大小、渲染时长和并发，确保定义与日志不含秘密。包版本面向特定 ActiveReports API 代际；升级时必须同步更新核心与 Web 厂商包，并重新测试浏览器客户端。

## 延伸阅读

- [核心适配器与当前限制](../README.zh-Hans.md)
- [Zongsoft.Reporting](../../../Zongsoft.Reporting/README.zh-Hans.md)
- [ActiveReports Web 集成](https://developer.mescius.com/activereportsnet/docs/introduction/getstarted)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Grapecity.Web` | [Zongsoft.Externals.Grapecity.Web.plugin](Zongsoft.Externals.Grapecity.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Grapecity.Web.deploy](Zongsoft.Externals.Grapecity.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft reporting]
nuget:Zongsoft.Reporting

[plugins zongsoft externals grapecity]
nuget:Zongsoft.Externals.Grapecity

[plugins zongsoft externals grapecity web]
nuget:Zongsoft.Externals.Grapecity.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Grapecity.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

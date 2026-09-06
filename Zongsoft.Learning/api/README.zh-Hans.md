# Zongsoft.Learning.Web 机器学习插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**L**earning.**W**eb](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Learning/api) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的机器学习插件库的子插件，提供了机器学习功能集的 _**W**eb_ 插件化支持。

当前包提供读取运行时机器学习管线目录的控制器，供开发和管理界面使用；不会训练模型、上传数据集、执行预测或持久化管线。控制器发现与 HTTP 路由映射是两个不同环节，当前源码并未提供可直接访问的默认路由。

## 安装与部署

```shell
dotnet add package Zongsoft.Learning.Web
```

请把 `Zongsoft.Learning.Web.plugin` 部署到插件化 Web 宿主。它依赖 `Zongsoft.Learning`；查询端点前，需要先加载核心插件以及所有注册管线目录或估算器的程序集。

🚨 当前源码与 Core 存在编译兼容性问题：`TextFileLoaderSettings.Populate` 的 `PropertyInfo` 参数与 Core 基类的 `MemberInfo` 签名不一致，定向构建报告 `CS0115`。修复或选用彼此兼容的版本前，不能把本页当作已经验证通过的部署教程；此次检查没有修改实现或运行训练。

## 端点

[PipelineController](Controllers/PipelineController.cs) 声明了 `MachineLearning` Area 和 `[HttpGet]`，但没有 `[Route]` 或 Zongsoft 的 `[ControllerName]`。默认 Plugins.Web 宿主只调用 `MapControllers()`，不会仅凭 Area 自动生成 URL。控制器动作计划返回：

- `Catalogs`：当前在 `Pipeline.Catalog` 中注册的目录组；
- `Estimators`：已注册的估算器/组件描述符。

因此不能假定 `GET /MachineLearning/Pipelines` 或单数形式已经可用。应用若接入本控制器，需要在组合层显式提供适用的 MVC 路由约定，或由应用自己的带路由控制器公开所需目录视图。应在宿主启动后检查端点集合并发起 HTTP 请求；OpenAPI 中没有端点时，先查路由元数据，不要直接归因于插件未加载。

> 💡 缺少估算器通常意味着其程序集或服务注册尚未加载；本 Web 包不会自行扫描任意目录或安装 ML.NET 组件。

## 安全与稳定性

🚨 目录元数据可能暴露模型能力和实现细节。非开发环境应保护该端点，并避免估算器描述符包含配置秘密。

响应是进程内注册项的快照。顺序和描述符细节属于发现元数据，而不是稳定的公开交换 Schema；只有宿主插件集合在缓存期内不变时才可缓存。

## 延伸阅读

- [机器学习概念与核心插件](../README.zh-Hans.md)
- [Zongsoft.Web 约定](../../Zongsoft.Web/README.zh-Hans.md)
- [ML.NET 文档](https://learn.microsoft.com/zh-cn/dotnet/machine-learning/)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Learning.Web` | [Zongsoft.Learning.Web.plugin](Zongsoft.Learning.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Learning.Web.deploy](Zongsoft.Learning.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft learning]
nuget:Zongsoft.Learning

[plugins zongsoft learning web]
nuget:Zongsoft.Learning.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Learning.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

# Zongsoft.Learning 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Zongsoft.Learning**](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Learning) 是 [Zongsoft](https://github.com/Zongsoft/framework) 应用框架的机器学习插件库。它基于 [ML.NET](https://dotnet.microsoft.com/zh-cn/apps/ai/ml-dotnet)，目标是为 Zongsoft 应用提供插件化的机器学习管线描述、发现与组装基础。

当前代码主要建立了数据集、管线步骤、训练器以及 ML.NET 组件注册等核心抽象和扩展点。

## 包

| 包名 | 说明 |
| ---- | ---- |
| `Zongsoft.Learning` | 核心机器学习插件库。 |
| `Zongsoft.Learning.Web` | 可选 Web 插件，为 Web 应用公开机器学习元数据。 |

## 当前范围

当前实现包含：

- 数据集与数据字段抽象。
- 管线与训练器步骤抽象。
- 用于注册 ML.NET 管线组件的运行时目录。
- 文本文件数据集加载器。
- 少量 ML.NET 数据转换器和训练器构建器。
- 用于集成 Zongsoft 框架的插件元数据和配置设置驱动。
- 面向后续持久化工作的初步数据库结构说明。

项目后续会继续向模型训练、管线管理、模型存储以及 Web 化能力演进。

## 目录结构

| 路径 | 说明 |
| ---- | ---- |
| `src/` | 核心 `Zongsoft.Learning` 插件库。 |
| `api/` | 可选 `Zongsoft.Learning.Web` 插件库。 |
| `database/` | 数据库结构草案。 |
| `build.cake` | 构建自动化脚本。 |

## 构建

还原并构建解决方案：

```powershell
dotnet restore Zongsoft.Learning.slnx
dotnet build Zongsoft.Learning.slnx
```

## 许可证

Zongsoft.Learning 基于 GNU Lesser General Public License 发布。详细信息请查看仓库许可证。

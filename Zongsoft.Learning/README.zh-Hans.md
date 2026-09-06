# Zongsoft.Learning 插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Learning)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Learning)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

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

## 面向应用开发者的基础概念

**IDataView** 是 ML.NET 的表格数据访问抽象，通常延迟求值。**估计器（Estimator）**描述转换或训练步骤，对其拟合得到**转换器（Transformer）**。流水线组合步骤，目录描述应用可选择的步骤。读取目录不等于训练模型。

当前插件注册 LightGbm 回归、列拼接、独热编码和独热哈希编码；其它目录分类可能为空，分类名称不代表存在可用训练器。

## 安装与安全入门示例

```shell
dotnet add package Zongsoft.Learning
```

在插件宿主中加载 `Zongsoft.Learning.plugin` 才会填充 `Pipeline.Catalog`。独立检查元数据不需要模型调用或训练数据：

```csharp
using Zongsoft.Learning;

static void PrintCatalog(EstimatorDescriptorCatalog catalog)
{
	foreach(var estimator in catalog.Estimators)
		Console.WriteLine(estimator.Name);

	foreach(var child in catalog.Catalogs)
		PrintCatalog(child);
}

PrintCatalog(Pipeline.Catalog);
```

没有插件初始化时，目录为空是正常结果；在完成配置的宿主中会列出注册的估计器名称。[Web 包](api/README.zh-Hans.md)暴露同一元数据。

## 数据加载与扩展点

`Dataset.Settings` 配置加载器，`DatasetLoader.GetLoader` 解析具名 `IDatasetLoader`。内置 `TextFile` 加载器从[设置类](src/Data/TextFileLoader.Settings.cs)构造 ML.NET TextLoader 选项。必须明确设置文件路径及列选项，仅声明 Dataset.Fields 不会自动构造这些选项。

新增步骤需实现 `IEstimatorBuilder` 并将描述符注册到对应目录。应用应区分数据加载、流水线构造、拟合、评估与模型存储。

## 当前限制

🚨 当前 `Pipeline.Build` 没有将后续步骤正确累积到返回的估计器链中，不应作为完整的多步训练工作流使用。未知估计器尚未转换为友好验证错误，空流水线可能返回 null。这些是当前实现限制，不是可依赖的支持行为。

数据库资料仍是草案，不是已安装的训练任务仓储；也没有完整的模型生命周期或训练 HTTP API。加载的数据可能稍后才枚举，应保证实际读取期间文件可用，并验证输入大小与列类型。

术语见 [ML.NET 概念](https://learn.microsoft.com/dotnet/machine-learning/resources/glossary)，实现约束见 [SKILL.md](SKILL.md)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

清单填充 `Pipeline.Catalog` 并注册加载器/估计器设置驱动。宿主初始化后可检查目录，加载插件不会训练模型或安装持久化服务。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Learning` | [Zongsoft.Learning.plugin](src/Zongsoft.Learning.plugin) |
| 文件复制及依赖 | [Zongsoft.Learning.deploy](src/Zongsoft.Learning.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft learning]
nuget:Zongsoft.Learning
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Learning.option`、`Zongsoft.Learning.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

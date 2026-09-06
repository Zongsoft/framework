# Zongsoft.Externals.OpenXml 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.OpenXml)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.OpenXml)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**OpenXml**](https://github.com/Zongsoft/framework/tree/main/externals/openxml) 在 [Open XML SDK](https://github.com/dotnet/Open-XML-SDK) 之上提供一组专注于电子表格的 API。

`Zongsoft.Externals.OpenXml.Spreadsheet` 命名空间包含创建和打开工作簿、枚举工作表、定位单元格以及读取或更新单元格值的辅助类型。`SpreadsheetDocument` 同时接受文件路径和数据流，因此既可处理本地文件，也可配合框架提供的存储使用。

可直接引用该程序包，或加载 `Zongsoft.Externals.OpenXml.plugin`；工作簿创建、单元格寻址和表格访问示例可参考[测试项目](test)。

## 安装与最小工作簿

```shell
dotnet add package Zongsoft.Externals.OpenXml
```

```csharp
using Zongsoft.Externals.OpenXml.Spreadsheet;

using var book = SpreadsheetDocument.Create("report.xlsx", "Summary");
book.Sheets[0].Cells.SetValue("A1", "Total");
book.Sheets[0].Cells.SetValue("B1", 42.5m);
book.Sheets[0].Cells.Merge("A2:B2");
book.Save();
```

`SpreadsheetDocument.Create` 接受路径或可写数据流；未提供名称时创建 `Sheet1`。`Open` 接受路径或数据流，默认只读，只有指定 `editable: true` 才可编辑。释放包装器会关闭底层 Open XML 包。

## 寻址与值

单元格辅助器使用 A1 地址和范围，支持设置值、读取文本、通过 `TryGetValue<T>` 转换值以及合并/取消合并范围。工作表可通过 `SheetCollection` 枚举和新增。

> 💡 Open XML 分别存储值、样式、共享字符串、公式、公式缓存结果、日期和数字格式。本包只覆盖常用单元格值操作；工作簿需要图表、高级样式、重新计算元数据、宏或其他包部件时，请使用底层 [Open XML SDK](https://learn.microsoft.com/zh-cn/office/open-xml/open-xml-sdk)。

## 限制与安全

本包不会启动 Excel，也不会计算公式。数据流必须符合 Open XML SDK 所需的可定位/可读/可写能力。除非调用者使用更底层流式 API，大型工作表会以文档树形式占用内存。

🚨 应把上传工作簿视为不可信 ZIP 包。请限制文件与展开后大小，在安全敏感流程中拒绝意外外部关系或宏，清理输出路径；覆盖源文件前务必保留可恢复副本。

## 延伸阅读

- [工作簿测试](test)
- [SpreadsheetML 概述](https://learn.microsoft.com/zh-cn/office/open-xml/spreadsheet/overview)
- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

插件加载工作簿适配程序集，应用仍需显式打开并释放每个工作簿；它不会自动创建电子表格转换服务或计算公式。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.OpenXml` | [Zongsoft.Externals.OpenXml.plugin](src/Zongsoft.Externals.OpenXml.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.OpenXml.deploy](src/Zongsoft.Externals.OpenXml.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals openxml]
nuget:Zongsoft.Externals.OpenXml
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.OpenXml.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

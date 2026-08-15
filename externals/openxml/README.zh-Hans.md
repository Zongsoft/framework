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

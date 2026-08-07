# Zongsoft.Externals.ClosedXml 扩展库

[![License](https://img.shields.io/github/license/Zongsoft/framework)](https://github.com/Zongsoft/framework/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Zongsoft.Externals.ClosedXml.svg)](https://www.nuget.org/packages/Zongsoft.Externals.ClosedXml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.ClosedXml.svg)](https://www.nuget.org/packages/Zongsoft.Externals.ClosedXml)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.ClosedXml` 将 [ClosedXML](https://github.com/ClosedXML/ClosedXML) 和 [ClosedXML.Report](https://github.com/ClosedXML/ClosedXML.Report) 集成到 Zongsoft 的数据归档与模板渲染抽象中，提供以下功能：

- 将模型数据导出为 `.xlsx` 工作簿；
- 从 `.xlsx` 工作簿提取强类型数据记录；
- 根据模型属性元数据生成枚举和布尔下拉框；
- 使用数据和参数渲染 Excel 报表模板；
- 从目录树中发现 `.xlsx` 模板；
- 提供英文和简体中文的验证及操作错误信息。

归档格式名为 `Spreadsheet`，文件扩展名为 `.xlsx`，MIME 类型为 `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`。

## 安装

安装 NuGet 包：

```shell
dotnet add package Zongsoft.Externals.ClosedXml
```

该包面向 Zongsoft Framework 所支持的目标框架，当前使用 ClosedXML `0.105.1` 和 ClosedXML.Report `0.2.12`。

## 工作簿约定

数据边界由 **Excel 表格（Table）** 定义，而不是名称（Defined Name）或工作表的已用区域。表格名必须等于模型描述器的 `Name`。

例如模型名为 `User`，工作簿中就必须包含名为 `User` 的 Excel 表格。通过这个约定，`Zongsoft.Data` 和 `Zongsoft.Web` 中的 `Import`/`ImportAsync` 端点可以直接根据当前模型定位数据集，不需要生成内部名称或增加额外配置。

工作表名只用于展示或分组。生成器使用 `model.Title ?? model.Name` 作为工作表名，提取器默认会搜索全部工作表。仅当需要将搜索限制在某个工作表时，才把 `DataArchiveExtractorOptions.Source` 设置为工作表名；其中的表格仍必须以模型名命名。

生成的布局如下：

| 行 | 内容 |
| --- | --- |
| 1 | 模型标题 |
| 2 | 导出时间和模型名 |
| 3 | Excel 表格标题行 |
| 4 及以后 | 数据记录 |

导出数据非空时，生成的表格严格包含实际导出的记录；没有任何导出记录时，则保留一个或多个空数据行以便人工录入，提取器会忽略这些全空行。

每个生成的标题单元格还会带有与模型字段同名的工作表级名称。提取器通过这些字段名稳定地映射列；对于人工创建的表格，也可以使用属性名作为标题进行后备匹配。

生成的表格将表头格式与数据区格式分开处理，数据区的灰色交替背景和行分隔线通过条件格式实现。用户在 Excel 中扩大表格时，这些规则会随数据区自动扩展，因此新增行无需自定义表格主题也能获得相同外观。

枚举属性会获得 Excel 数据验证下拉框，其条目为枚举成员名；布尔属性使用 `TRUE`/`FALSE` 下拉框；可空枚举和可空布尔属性还会包含一个可选的空条目。需要真实空白项或原生布尔值的列表集中存放在 VeryHidden 内部工作表中，并由数据验证直接引用该来源区域；这样既不会在可编辑的数据表右侧添加辅助列，也避免 Excel 把原生布尔单元格误判为不属于文本列表。Excel 默认日期系统无法正确显示 `1900-01-01` 以前的日期，因此这些日期会写成可读的 `yyyy-MM-dd` 文本；受支持的日期仍保持原生 Excel 日期值。

简单属性元数据还会生成 Excel 原生输入验证。字符字段的 `Length` 大于零时会拒绝超长内容，非空字符字段还会拒绝空值；Byte 至 UInt32 字段只允许对应类型范围内的整数；Decimal、Currency、VarNumeric、Single 和 Double 字段只允许数值。DateTime 字段使用 Excel 原生日期验证，允许 `1900-01-01` 到 `9999-12-31` 之间的日期，不支持更早的日期。验证使用本地化的“停止”级错误提示，并遵循可空元数据。Int64/UInt64、Guid、二进制、对象、XML 和 JSON 因 Excel 精度或原生验证能力不足，仍由导入阶段校验。Excel 验证只是录入阶段的辅助措施，复制粘贴、宏或外部写入仍可能绕过它，因此最终以提取和模型验证为准。

生成列的宽度遵循简单属性的 `DataType` 和语义 `Role`。字符列还会结合声明的 `Length`，并使用适宜的最小、默认宽度及最大宽度 50。`Role` 为 `Currency` 的属性使用 Excel 随区域自适应的内置货币格式。主键数据列采用居中对齐、粗体和 Maroon 字体颜色。主键、枚举、日期、布尔值、标识符及适用语义角色的居中对齐还会保存为工作表列的默认样式，因此用户扩大表格后在新增行中录入的值仍会保持相同对齐。

### 编辑后的范围恢复

用户有时会在表格下方追加记录，却没有同步扩大 Excel 表格。对于未启用汇总行的表格，提取器会保持表格的列边界，并把最后一行向下扩展至这些列中的最后一个非空单元格；空行会被忽略。这样既能恢复常见的人工编辑，又不会把无关列当成模型数据。

请把备注和无关内容放在表格列带之外：这些列下方的内容可能会被有意识别为追加记录。启用汇总行后，提取器只读取表格声明的数据区域。

如果文件仅包含模型级名称、无效引用，或者只有与模型同名的工作表，提取器会明确拒绝该文件。请创建一个实际的 Excel 表格，并把表格名设置为模型名。

## 导出数据

`SpreadsheetGenerator` 会在一次操作中创建工作簿和模型表格：

```csharp
using Zongsoft.Data;
using Zongsoft.Externals.ClosedXml;

var model = Model.GetDescriptor<User>();
var users = GetUsers();

await using var output = File.Create("users.xlsx");
await new SpreadsheetGenerator().GenerateAsync(output, model, users);
```

可通过 `DataArchiveGeneratorOptions` 选择要导出的字段：

```csharp
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(nameof(User.UserId), nameof(User.Name));
await generator.GenerateAsync(output, model, users, options);
```

由于模型名会成为 Excel 表格名，因此必须满足 Excel 的表格命名规则。如果名称无效，生成器会报告本地化的验证错误。

## 提取数据

`SpreadsheetExtractor` 从提取选项中取得模型，定位以该模型命名的表格，并把表格列映射回模型属性：

```csharp
using Zongsoft.Data;
using Zongsoft.Data.Archiving;
using Zongsoft.Externals.ClosedXml;

var model = Model.GetDescriptor<User>();
var options = new DataArchiveExtractorOptions(model);

await using var input = File.OpenRead("users.xlsx");
await foreach(var user in new SpreadsheetExtractor().ExtractAsync<User>(input, options))
	Console.WriteLine($"{user.UserId}: {user.Name}");
```

将查找范围限制到指定工作表：

```csharp
var options = new DataArchiveExtractorOptions(model)
{
	Source = "Import",
};
```

如果无法解析工作表、模型表格或必需的模型字段，提取器会报告本地化错误。

## Zongsoft.Web 集成

生成器和提取器分别注册为 `IDataArchiveGenerator` 和 `IDataArchiveExtractor` 服务。应用加载该扩展后，`ServiceController` 的导入操作会把当前模型描述器提供给提取器。因此默认端点的契约很简单：上传的工作簿中必须包含以当前模型名命名的 Excel 表格。

不需要使用私有生成的表格名；自定义工作表名也不会改变模型表格的命名约定。

## 渲染模板

`SpreadsheetRenderer` 使用 ClosedXML.Report 变量渲染 `.xlsx` 模板。`SpreadsheetTemplateProvider` 会递归发现 `.xlsx` 文件，并以不含扩展名的文件名作为模板索引：

```csharp
using Zongsoft.Externals.ClosedXml;

var provider = new SpreadsheetTemplateProvider("templates");
var template = provider.GetTemplate("invoice")
	?? throw new InvalidOperationException("Template not found.");

var parameters = new Dictionary<string, object>
{
	["GeneratedAt"] = DateTimeOffset.Now,
};

await using var output = File.Create("invoice.xlsx");
await new SpreadsheetRenderer().RenderAsync(output, template, invoice, parameters);
```

模板变量和表达式遵循 [ClosedXML.Report](https://github.com/ClosedXML/ClosedXML.Report) 语法。

## 本地化

默认资源语言为英文，并提供 `zh-Hans` 简体中文资源。错误信息跟随 `CultureInfo.CurrentUICulture`，应用应通过常规的请求或宿主本地化管线设置界面区域性。

## 范例

交互式[范例项目](samples/Program.cs)使用 [Bogus](https://github.com/bchavez/Bogus) 生成与语言文化匹配的用户假数据，随后导出、重新导入并显示工作簿结构和提取结果，便于人工校验。

在仓库根目录运行：

```shell
dotnet run --project externals/closedxml/samples/Zongsoft.Externals.ClosedXml.Samples.csproj -f net10.0
```

可用命令：

| 命令 | 说明 |
| --- | --- |
| `export [--count:<number>\|-c:<number>] [--culture:<name>\|-l:<name>] [file]` | 生成并导出用户假数据，然后显示生成的工作表、表格、范围、列数和行数。例如 `export -c:20 -l:zh-Hans users.xlsx`。 |
| `import [file]` | 导入工作簿，并显示其结构和提取到的用户。 |
| `verify [options] [file]` | 导出后立即导入工作簿，完成端到端检查；支持与 `export` 相同的记录数和语言文化选项。 |

`export` 或 `verify` 未指定文件参数时，默认文件名会包含实际生效的语言文化和记录数，譬如 `users.zh-CN(10).xlsx` 或 `users.en(0).xlsx`。默认生成 10 条记录；记录数为零时，会创建供人工录入的空白行。`import` 仍默认使用 `users.xlsx`。`out` 和 `in` 分别是 `export` 和 `import` 的别名。

例如，`export --count:20 --culture:en-US users.en.xlsx` 会生成英文标题、标签和用户假数据，`export -c:20 -l:zh-Hans users.zh-Hans.xlsx` 则会生成简体中文内容。指定的文化仅对当次命令生效。

## 构建和测试

```shell
dotnet build externals/closedxml/Zongsoft.Externals.ClosedXml.slnx --no-incremental
dotnet test externals/closedxml/test/Zongsoft.Externals.ClosedXml.Tests.csproj -f net10.0
```

## 许可证

本项目采用 [GNU 宽通用公共许可证](../../LICENSE)。

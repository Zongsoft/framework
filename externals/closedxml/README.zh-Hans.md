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

应用插件只需引用公共归档契约；实现包由宿主[插件部署](#插件化接入)，不要求业务模块直接依赖 ClosedXML：

```shell
dotnet add package Zongsoft.Core
```

该包面向 Zongsoft Framework 所支持的目标框架，当前使用 ClosedXML `0.105.1` 和 ClosedXML.Report `0.2.12`。

## 工作簿约定

数据边界由 **Excel 表格（Table）** 定义，而不是名称（Defined Name）或工作表的已用区域。[当前命名规则](src/Spreadsheet.cs)为 `__{model.QualifiedName}__`：两端各有两个下划线，限定名包含所属模块。

真实的[测试 User 模型](test/Models/User.cs)及 [Templates 夹具](test/Templates.cs)展示无模块模型；真实的 [Discussions Forum 模型](../../../discussions/src/Models/Forum.cs)属于 Discussions 模块。生成器使用描述器限定名，而非任意 CLR 命名空间。CLR 命名空间本身不能声明模块归属，见 [ModelDescriptor](../../Zongsoft.Core/src/Data/ModelDescriptor.cs)。

工作表名只用于展示或分组。生成器使用非空白的 `model.Title`，否则使用 `model.Name`；提取器默认搜索全部工作表。仅当需要限制查找范围时，才把 `DataArchiveExtractorOptions.Source` 设置为工作表名；这不会改变内部表格名。

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

🚨 工作表或 Defined Name 不能代替实际 Excel 表格。旧模板中普通的 `User` 表格名不会自动回退匹配，应先改为与目标模型对应的内部表格名。

## 导出数据

在已启动并加载 ClosedXml 插件的命令或应用服务中，通过格式名 `Spreadsheet` 匹配公共接口；模块内可改用 `Module.Current.Services`。

以下按现有 [Discussions Forum](../../../discussions/src/Models/Forum.cs) 类型改写，不另建 User 类。在宿主装配完成后的 Discussions 消费模块中使用；空数组用于生成空白录入工作簿，不读取真实业务记录：

```csharp
using Zongsoft.Data;
using Zongsoft.Data.Archiving;
using Zongsoft.Services;
using Zongsoft.Discussions.Models;

var generator = ApplicationContext.Current.Services
	.FindRequired<IDataArchiveGenerator>("Spreadsheet");
var model = Model.GetDescriptor<Forum>();
var forums = Array.Empty<Forum>();

using var output = new MemoryStream();
await generator.GenerateAsync(output, model, forums);
```

调用方负责输出流的生命周期；不要为一次操作释放容器共享的服务。实际数据服务应使用 `service.GetDescriptor()`，这样映射中的主键、长度等信息才会纳入描述器；单独的 `Model.GetDescriptor<Forum>()` 只反映类型声明。

💡 仓库内可追踪的往返用例见 [SpreadsheetExtractorTest](test/SpreadsheetExtractorTest.cs)，使用 [Templates](test/Templates.cs) 中的 User 数据与描述器。应阅读这些断言，不依赖仓库外临时探针。这里的数据是测试夹具，不是用户工作簿。

可在上述 `GenerateAsync` 调用处通过 `DataArchiveGeneratorOptions` 选择字段：

```csharp
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(nameof(Forum.ForumId), nameof(Forum.Name));
await generator.GenerateAsync(output, model, forums, options);
```

如果要显式控制列的显示方式，可传入 `DataArchiveField`。宽度统一使用排版点（1/72 英寸），零表示未指定；字体大小同样约定零表示未指定。颜色使用 `Zongsoft.Components.Color` 提供的技术无关 ARGB 值，空值表示未指定颜色；`Format` 是 .NET 格式说明符，而不是 Excel 数字格式代码：

```csharp
using Zongsoft.Components;
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(
	new DataArchiveField(nameof(Forum.ForumId))
	{
		Width = 72,
		Alignment = DataArchiveFieldAlignment.Center,
		FontStyle = DataArchiveFontStyle.Bold,
		ForegroundColor = Color.Maroon,
	},
	new DataArchiveField(nameof(Forum.TotalThreads))
	{
		Width = 90,
		Alignment = DataArchiveFieldAlignment.Right,
		Format = "N0",
	},
	new DataArchiveField(nameof(Forum.Description))
	{
		Width = 180,
		TextMode = DataArchiveFieldTextMode.Wrap,
	});
```

未指定的选项继续使用根据模型元数据推导的样式。`None`、`Wrap` 和 `Shrink` 分别对应 Excel 原生的不换行、自动换行和缩小字体行为。由于 Excel 单元格无法在不改变实际值的情况下原生显示末尾省略号，因此不提供省略号模式。

生成后的完整内部表格名必须满足 Excel 表格命名规则。如果名称无效，生成器会报告本地化的验证错误。

## 提取数据

`IDataArchiveExtractor` 从提取选项中取得模型，定位内部表格，并把表格列映射回模型属性。下面沿用上一节的 `Forum` 类型：

```csharp
var extractor = ApplicationContext.Current.Services
	.FindRequired<IDataArchiveExtractor>("Spreadsheet");
output.Position = 0;
var extractionOptions = new DataArchiveExtractorOptions(model);

await foreach(var forum in extractor.ExtractAsync<Forum>(output, extractionOptions))
	Console.WriteLine($"{forum.ForumId}: {forum.Name}");
```

将查找范围限制到指定工作表：

```csharp
var options = new DataArchiveExtractorOptions(model)
{
	Source = string.IsNullOrWhiteSpace(model.Title) ? model.Name : model.Title,
};
```

如果无法解析工作表、模型表格或必需的模型字段，提取器会报告本地化错误。

## Zongsoft.Web 集成

生成器和提取器分别注册为 `IDataArchiveGenerator` 和 `IDataArchiveExtractor`。应用加载该扩展后，`ServiceController` 的导入操作把当前模型描述器提供给提取器；上传的工作簿必须包含与该描述器的 `QualifiedName` 对应的内部 Excel 表格。

💡 优先使用同一模型的导出模板作为导入起点，避免手工猜测表格名、字段和元数据。重命名工作表不等于重命名 Excel 表格。

## 渲染模板

`SpreadsheetRenderer` 使用 ClosedXML.Report 变量渲染 `.xlsx` 模板。`SpreadsheetTemplateProvider` 会递归发现 `.xlsx` 文件，并以不含扩展名的文件名作为模板索引：

真实的 [SpreadsheetRendererTest](test/SpreadsheetRendererTest.cs) 使用 [Templates.ApartmentUsage](test/Templates.cs) 夹具渲染 [apartment.usages.xlsx](test/templates/apartment.usages.xlsx)。以下摘录其渲染部分；`_renderer`、`Templates` 属于测试项目，不是公共包类型：

```csharp
using var output = new MemoryStream();
var data = new { Templates.ApartmentUsage.Usages };
var parameters = new[]
{
	new KeyValuePair<string, object>(nameof(Templates.ApartmentUsage.Park), Templates.ApartmentUsage.Park),
};

await _renderer.RenderAsync(output, Templates.ApartmentUsage.Template, data, parameters);
```

测试断言生成单元格中的园区标题、房间/设备字段、日期及用量合计。宿主消费者通过服务容器按 `Spreadsheet` 格式名取得 `IDataTemplateProvider` 与 `IDataTemplateRenderer`，并提供与已部署模板匹配的数据，不能假定存在发票模板字段。

默认提供者在首次查找时递归扫描应用目录并缓存索引，不持续监视文件，也不读取 `templates` 配置。查找前部署唯一文件名的模板；测试夹具则显式使用自己的测试模板目录。输出应放在模板扫描范围之外或内存中，绝不能覆盖源工作簿。

🚨 工作簿处理会在内存中展开文件；`ValueTask` 返回类型不表示完全异步或支持随时中断。限制上传大小、行数及并发，不把 Excel 数据验证当作服务端业务校验。

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

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

服务扫描注册归档生成器、提取器、模板提供者与渲染器；按 `Spreadsheet` 匹配公共接口。模板和数据由应用提供，插件加载不会自动生成工作簿。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.ClosedXml` | [Zongsoft.Externals.ClosedXml.plugin](src/Zongsoft.Externals.ClosedXml.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.ClosedXml.deploy](src/Zongsoft.Externals.ClosedXml.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals closedxml]
nuget:Zongsoft.Externals.ClosedXml
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

🚨 当前源码的 `.csproj` 引用 ClosedXML `0.105.1` / ClosedXML.Report `0.2.12`，但 `.deploy` 仍指定 `0.102.2` / `0.2.10`。不能将该清单视为当前源码构建的完整依赖集合；隔离验证时使用与构建资产一致的依赖，并检查最终 DLL 版本。补齐依赖前不要据此直接投产。

清单列出的附属产物包括：`Zongsoft.Externals.ClosedXml.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

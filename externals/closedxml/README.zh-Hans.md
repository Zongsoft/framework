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

例如无模块的 `User` 对应 `__User__`，`Sales.User` 对应 `__Sales.User__`。生成器自动设置该名称；人工制作模板时也必须遵守。不要把 CLR 命名空间直接当作模型模块名，限定名的来源见 [ModelDescriptor](../../Zongsoft.Core/src/Data/ModelDescriptor.cs)。

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

在已启动并加载 ClosedXml 插件的命令或应用服务中，通过格式名 `Spreadsheet` 匹配公共接口；模块内可改用 `Module.Current.Services`。以下示例导出一个自包含的普通模型，不需要数据库：

```csharp
using Zongsoft.Data;
using Zongsoft.Data.Archiving;
using Zongsoft.Services;

var generator = ApplicationContext.Current.Services
	.FindRequired<IDataArchiveGenerator>("Spreadsheet");
var model = Model.GetDescriptor<User>();
var users = new[]
{
	new User { UserId = 1, Name = "Alice", Balance = 12.50m, Email = "alice@example.invalid" },
};

await using var output = File.Create("users.xlsx");
await generator.GenerateAsync(output, model, users);

public class User
{
	public int UserId { get; set; }
	public string Name { get; set; }
	public decimal Balance { get; set; }
	public string Email { get; set; }
}
```

调用方负责输出流的生命周期；不要为一次操作释放容器共享的服务。实际数据服务应使用 `service.GetDescriptor()`，这样映射中的主键、长度等信息才会纳入描述器；单独的 `Model.GetDescriptor<User>()` 只反映类型声明。

💡 该公共接口路径已在隔离终端宿主中完成内存流往返验证：属于 Docs 模块的 User 生成 `__Docs.User__` 表格，提取后记录数量及字段值一致。没有读写用户工作簿；这不代替大文件、模板表达式或所有单元格类型的测试。

可在上述 `GenerateAsync` 调用处通过 `DataArchiveGeneratorOptions` 选择字段：

```csharp
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(nameof(User.UserId), nameof(User.Name));
await generator.GenerateAsync(output, model, users, options);
```

如果要显式控制列的显示方式，可传入 `DataArchiveField`。宽度统一使用排版点（1/72 英寸），零表示未指定；字体大小同样约定零表示未指定。颜色使用 `Zongsoft.Components.Color` 提供的技术无关 ARGB 值，空值表示未指定颜色；`Format` 是 .NET 格式说明符，而不是 Excel 数字格式代码：

```csharp
using Zongsoft.Components;
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(
	new DataArchiveField(nameof(User.UserId))
	{
		Width = 72,
		Alignment = DataArchiveFieldAlignment.Center,
		FontStyle = DataArchiveFontStyle.Bold,
		ForegroundColor = Color.Maroon,
	},
	new DataArchiveField(nameof(User.Balance))
	{
		Width = 90,
		Alignment = DataArchiveFieldAlignment.Right,
		Format = "N2",
	},
	new DataArchiveField(nameof(User.Email))
	{
		Width = 180,
		TextMode = DataArchiveFieldTextMode.Wrap,
	});
```

未指定的选项继续使用根据模型元数据推导的样式。`None`、`Wrap` 和 `Shrink` 分别对应 Excel 原生的不换行、自动换行和缩小字体行为。由于 Excel 单元格无法在不改变实际值的情况下原生显示末尾省略号，因此不提供省略号模式。

生成后的完整内部表格名必须满足 Excel 表格命名规则。如果名称无效，生成器会报告本地化的验证错误。

## 提取数据

`IDataArchiveExtractor` 从提取选项中取得模型，定位内部表格，并把表格列映射回模型属性。下面沿用上一节的 `User` 类型：

```csharp
using Zongsoft.Data;
using Zongsoft.Data.Archiving;
using Zongsoft.Services;

var extractor = ApplicationContext.Current.Services
	.FindRequired<IDataArchiveExtractor>("Spreadsheet");
var model = Model.GetDescriptor<User>();
var options = new DataArchiveExtractorOptions(model);

await using var input = File.OpenRead("users.xlsx");
await foreach(var user in extractor.ExtractAsync<User>(input, options))
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

生成器和提取器分别注册为 `IDataArchiveGenerator` 和 `IDataArchiveExtractor`。应用加载该扩展后，`ServiceController` 的导入操作把当前模型描述器提供给提取器；上传的工作簿必须包含与该描述器的 `QualifiedName` 对应的内部 Excel 表格。

💡 优先使用同一模型的导出模板作为导入起点，避免手工猜测表格名、字段和元数据。重命名工作表不等于重命名 Excel 表格。

## 渲染模板

`SpreadsheetRenderer` 使用 ClosedXML.Report 变量渲染 `.xlsx` 模板。`SpreadsheetTemplateProvider` 会递归发现 `.xlsx` 文件，并以不含扩展名的文件名作为模板索引：

```csharp
using Zongsoft.Data.Archiving;
using Zongsoft.Services;

var services = ApplicationContext.Current.Services;
var provider = services.FindRequired<IDataTemplateProvider>("Spreadsheet");
var renderer = services.FindRequired<IDataTemplateRenderer>("Spreadsheet");
var template = provider.GetTemplate("invoice")
	?? throw new InvalidOperationException("Template not found.");

var invoice = new { Number = "DEMO-001", Total = 12.50m };
var parameters = new Dictionary<string, object>
{
	["GeneratedAt"] = DateTimeOffset.Now,
};

using var output = new MemoryStream();
await renderer.RenderAsync(output, template, invoice, parameters);
```

默认模板提供者在首次查找时递归扫描应用目录下的 `.xlsx`，并缓存索引；不是自动读取 `templates` 配置，也不会持续监视新文件。预先放置唯一命名的 `invoice.xlsx`，其中可以使用 `{{Number}}`、`{{Total}}` 和 `{{GeneratedAt}}`；同名文件不会按目录隔离。示例输出使用内存流，避免覆盖模板或让输出文件被误认为模板。自定义根目录应由宿主组合专用提供者，业务模块仍消费公共接口。

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

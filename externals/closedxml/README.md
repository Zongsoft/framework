# Zongsoft.Externals.ClosedXml Extension Library

[![License](https://img.shields.io/github/license/Zongsoft/framework)](https://github.com/Zongsoft/framework/blob/main/LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Zongsoft.Externals.ClosedXml.svg)](https://www.nuget.org/packages/Zongsoft.Externals.ClosedXml)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.ClosedXml.svg)](https://www.nuget.org/packages/Zongsoft.Externals.ClosedXml)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.ClosedXml` integrates [ClosedXML](https://github.com/ClosedXML/ClosedXML) and [ClosedXML.Report](https://github.com/ClosedXML/ClosedXML.Report) with the data archiving and template rendering abstractions provided by Zongsoft. It supports:

- Exporting model data to `.xlsx` workbooks;
- Extracting strongly typed records from `.xlsx` workbooks;
- Creating enum and Boolean drop-down lists from model property metadata;
- Rendering Excel report templates with data and parameters;
- Discovering `.xlsx` templates from a directory tree;
- Localized validation and operation errors in English and Simplified Chinese.

The archive format is named `Spreadsheet`, uses the `.xlsx` extension, and has the MIME type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`.

## Installation

Application plugins reference the shared archiving contracts. Deploy the implementation through the [plugin host](#plugin-based-integration), without making business modules depend on ClosedXML:

```shell
dotnet add package Zongsoft.Core
```

The package targets the same supported frameworks as Zongsoft Framework and currently uses ClosedXML `0.105.1` and ClosedXML.Report `0.2.12`.

## Workbook Convention

The data boundary is an **Excel Table**, not a Defined Name or the worksheet's used range. The [current naming rule](src/Spreadsheet.cs) is `__{model.QualifiedName}__`: two underscores at each end, with the module included in the qualified model name.

For example, an unqualified `User` maps to `__User__`, while `Sales.User` maps to `__Sales.User__`. The generator sets this name automatically; hand-authored templates must follow it too. A CLR namespace is not automatically the model's module name; see [ModelDescriptor](../../Zongsoft.Core/src/Data/ModelDescriptor.cs).

The worksheet name is only a display or grouping concern. The generator uses a nonblank `model.Title`, otherwise `model.Name`; the extractor searches all worksheets by default. Set `DataArchiveExtractorOptions.Source` to a worksheet name to restrict lookup; this does not change the internal Table name.

The generated layout is:

| Row | Content |
| --- | --- |
| 1 | Model title |
| 2 | Export time and model name |
| 3 | Excel Table header |
| 4 and below | Data records |

For non-empty exports, the generated Table contains exactly the exported records. When no records are exported, it contains one or more blank data rows for manual entry; the extractor ignores those completely empty rows.

Each generated header cell also has a worksheet-scoped Defined Name matching its model field. The extractor uses these field names for stable column mapping and accepts property-name headers as a fallback for manually created tables.

The generated Table keeps its header formatting separate and uses conditional formatting for the data area's alternating gray background and row separators. Excel automatically expands these rules when a user resizes the Table, so newly added rows receive the same data-area appearance without a custom Table theme.

Enum properties receive an Excel data-validation drop-down whose entries are the enum member names. Boolean properties use a `TRUE`/`FALSE` drop-down. Nullable enum and Boolean properties additionally contain one selectable empty entry. Lists that require a real blank or typed Boolean values are stored on a VeryHidden internal worksheet and referenced directly as validation ranges; this keeps validation infrastructure out of the editable data sheet and prevents Excel from treating native Boolean cells as invalid text-list values. Dates before `1900-01-01`, which Excel's default date system cannot display correctly, are written as readable `yyyy-MM-dd` text; supported dates remain native Excel date values.

Simplex metadata also drives native Excel input validation. Character fields with a positive `Length` reject longer input, and non-nullable character fields additionally reject empty input; Byte through UInt32 fields require whole numbers within their type range; Decimal, Currency, VarNumeric, Single, and Double fields require numeric input. DateTime fields use Excel's native date validation and accept dates from `1900-01-01` through `9999-12-31`; earlier dates are unsupported. These rules use localized Stop-style error messages and respect nullable metadata. Int64/UInt64, Guid, binary, object, XML, and JSON values intentionally remain import-validated because Excel precision or reliable native validation is insufficient. Excel validation is an early-entry aid and can be bypassed by paste, macros, or external writers, so extraction and model validation remain authoritative.

Generated column widths follow simplex-property `DataType` and semantic `Role`. Character columns also use their declared `Length`, with practical minimum/default widths and a maximum width of 50. Properties whose role is `Currency` use Excel's locale-aware built-in currency format. Primary-key data columns are centered and use bold Maroon text. Center alignment for keys, enums, dates, Boolean values, identifiers and applicable semantic roles is also stored as the worksheet-column default, so values entered into rows added by resizing the Table retain the same alignment.

### Edited workbook recovery

Excel users sometimes append records below a table without expanding it. When the table does not use a totals row, the extractor keeps the table's column boundary but extends the last data row to the last non-empty cell below those columns. Empty rows are ignored. This recovers common edits without treating unrelated columns as model data.

Keep notes and unrelated content outside the table's column band: content below those columns can intentionally be interpreted as an appended record. When a totals row is enabled, only the table's declared data range is extracted.

🚨 A worksheet or Defined Name cannot replace an actual Excel Table. Legacy Tables simply named `User` are not matched as a fallback; update their internal Table name to match the target model.

## Exporting Data

Inside a command or application service of a running host with the ClosedXml plugin loaded, match the public contract by format name `Spreadsheet`. A module can use `Module.Current.Services` instead. This self-contained plain-model example needs no database:

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

The caller owns the output stream; do not dispose a container-owned service after each operation. For an actual data service, use `service.GetDescriptor()` to include mapping metadata such as keys and lengths; `Model.GetDescriptor<User>()` alone reflects type declarations.

💡 This shared-interface path passed an in-memory round trip in an isolated terminal host: a User in the Docs module produced `__Docs.User__`, and extraction preserved the record count and field values. No user workbook was read or written. This does not replace testing large files, template expressions or every cell type.

At the `GenerateAsync` call above, use `DataArchiveGeneratorOptions` to select fields:

```csharp
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(nameof(User.UserId), nameof(User.Name));
await generator.GenerateAsync(output, model, users, options);
```

For explicit column presentation, pass `DataArchiveField` instances. Widths use typographic points (1/72 inch), with zero meaning unspecified; font sizes follow the same zero-as-unspecified convention. Colors are technology-neutral ARGB values from `Zongsoft.Components.Color`, while a null color means unspecified; and `Format` is a .NET format specifier rather than an Excel number-format code:

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

Unspecified options continue to use styles inferred from model metadata. `None`, `Wrap`, and `Shrink` map to Excel's native text-display behaviors. An ellipsis mode is intentionally absent because Excel cells cannot display a native trailing ellipsis without changing the stored value.

The complete generated internal Table name must satisfy Excel's table-name rules. The generator reports a localized validation error when it does not.

## Extracting Data

`IDataArchiveExtractor` obtains the model from extraction options, locates its internal Table, and maps columns back to model properties. The following uses the `User` type from the previous section:

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

To restrict lookup to a specific worksheet:

```csharp
var options = new DataArchiveExtractorOptions(model)
{
	Source = "Import",
};
```

The extractor reports a localized error when the worksheet, model table, or required model fields cannot be resolved.

## Zongsoft.Web Integration

The generator and extractor are registered as `IDataArchiveGenerator` and `IDataArchiveExtractor`. After the extension is loaded, a `ServiceController` import operation supplies its current model descriptor; the workbook must contain the internal Excel Table corresponding to that descriptor's `QualifiedName`.

💡 Start imports from an exported template for the same model, avoiding guesses about Table names, fields and metadata. Renaming a worksheet does not rename its Excel Table.

## Rendering Templates

`SpreadsheetRenderer` renders an `.xlsx` template using ClosedXML.Report variables. `SpreadsheetTemplateProvider` recursively discovers `.xlsx` files and indexes each template by its filename without the extension:

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

On first lookup, the default template provider recursively scans the application directory for `.xlsx` files and caches the index; it does not read a `templates` setting or continuously watch for new files. Place a uniquely named `invoice.xlsx` beforehand, using cells such as `{{Number}}`, `{{Total}}` and `{{GeneratedAt}}`. Duplicate filenames are not isolated by directory. The example writes to memory to avoid overwriting a template or discovering generated output as a template. A custom root requires a host-composed provider; business modules still consume the shared interface.

🚨 Workbook processing expands files in memory. A `ValueTask` return type does not imply fully asynchronous or interruptible processing. Limit upload size, row count and concurrency; Excel validation is not server-side business validation.

Template variables and expressions follow the [ClosedXML.Report](https://github.com/ClosedXML/ClosedXML.Report) syntax.

## Localization

English is the neutral resource language, and Simplified Chinese resources are provided for `zh-Hans`. Error messages follow `CultureInfo.CurrentUICulture`, so applications should establish the UI culture through their normal request or host localization pipeline.

## Sample

The interactive [sample project](samples/Program.cs) uses [Bogus](https://github.com/bchavez/Bogus) to generate locale-aware fake users, exports data, imports it again, and displays the workbook structure and extracted records for manual verification.

Run it from the repository root:

```shell
dotnet run --project externals/closedxml/samples/Zongsoft.Externals.ClosedXml.Samples.csproj -f net10.0
```

Available commands:

| Command | Description |
| --- | --- |
| `export [--count:<number>\|-c:<number>] [--culture:<name>\|-l:<name>] [file]` | Generate and export fake users, then display the generated worksheet, table, range, columns, and rows. For example, `export -c:20 -l:en-US users.xlsx`. |
| `import [file]` | Import a workbook and display its structure and extracted users. |
| `verify [options] [file]` | Export and immediately import a workbook for an end-to-end check; it accepts the same count and culture options as `export`. |

When `export` or `verify` has no file argument, its default name includes the effective culture and record count, such as `users.zh-CN(10).xlsx` or `users.en(0).xlsx`. The default generated record count is 10; a count of zero creates the generator's blank entry rows. `import` still defaults to `users.xlsx`. `out` and `in` are aliases for `export` and `import`.

For example, `export --count:20 --culture:en-US users.en.xlsx` generates English titles, labels, and fake user names, while `export -c:20 -l:zh-Hans users.zh-Hans.xlsx` generates Simplified Chinese content. The selected culture applies only to that command.

## Build and Test

```shell
dotnet build externals/closedxml/Zongsoft.Externals.ClosedXml.slnx --no-incremental
dotnet test externals/closedxml/test/Zongsoft.Externals.ClosedXml.Tests.csproj -f net10.0
```

## License

This project is licensed under the [GNU Lesser General Public License](../../LICENSE).

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Service scanning registers archive generators, extractors, template providers and renderers; match their shared interfaces by `Spreadsheet`. Templates and data are application inputs, not workbooks generated by plugin loading.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.ClosedXml` | [Zongsoft.Externals.ClosedXml.plugin](src/Zongsoft.Externals.ClosedXml.plugin) |
| File copying and dependencies | [Zongsoft.Externals.ClosedXml.deploy](src/Zongsoft.Externals.ClosedXml.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals closedxml]
nuget:Zongsoft.Externals.ClosedXml
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

🚨 The current `.csproj` references ClosedXML `0.105.1` / ClosedXML.Report `0.2.12`, but `.deploy` still specifies `0.102.2` / `0.2.10`. That manifest is not a complete dependency recipe for a build of the current source. In isolated verification, use dependencies matching the build assets and check the final DLL versions. Resolve this mismatch before production deployment.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.ClosedXml.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

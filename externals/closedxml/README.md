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

Install the NuGet package:

```shell
dotnet add package Zongsoft.Externals.ClosedXml
```

The package targets the same supported frameworks as Zongsoft Framework and currently uses ClosedXML `0.105.1` and ClosedXML.Report `0.2.12`.

## Workbook Convention

The data boundary is an **Excel Table**, not a Defined Name or the worksheet's used range. The table name must be the model descriptor's `Name`.

For a model whose name is `User`, the workbook therefore contains an Excel Table named `User`. This convention lets `Zongsoft.Data` and the `Import`/`ImportAsync` endpoints in `Zongsoft.Web` locate the dataset directly from the current model without requiring a generated internal name or extra configuration.

The worksheet name is only a display or grouping concern. The generator uses `model.Title ?? model.Name` for it, while the extractor searches all worksheets by default. Set `DataArchiveExtractorOptions.Source` to a worksheet name only when the search must be restricted to that worksheet; the table inside it must still be named after the model.

The generated layout is:

| Row | Content |
| --- | --- |
| 1 | Model title |
| 2 | Export time and model name |
| 3 | Excel Table header |
| 4 and below | Data records |

The generated Table always contains at least 10 data rows. When fewer than 10 records are exported, the remaining rows are left blank for manual entry; the extractor ignores those completely empty rows.

Each generated header cell also has a worksheet-scoped Defined Name matching its model field. The extractor uses these field names for stable column mapping and accepts property-name headers as a fallback for manually created tables.

The generated Table keeps its header formatting separate and uses conditional formatting for the data area's alternating gray background and row separators. Excel automatically expands these rules when a user resizes the Table, so newly added rows receive the same data-area appearance without a custom Table theme.

Enum properties receive an Excel data-validation drop-down whose entries are the enum member names. Boolean properties use a `TRUE`/`FALSE` drop-down. Nullable enum and Boolean properties additionally contain one selectable empty entry. Lists that require a real blank or typed Boolean values are stored on a VeryHidden internal worksheet and referenced directly as validation ranges; this keeps validation infrastructure out of the editable data sheet and prevents Excel from treating native Boolean cells as invalid text-list values. Dates before `1900-01-01`, which Excel's default date system cannot display correctly, are written as readable `yyyy-MM-dd` text; supported dates remain native Excel date values.

Simplex metadata also drives native Excel input validation. Character fields with a positive `Length` reject longer input; Byte through UInt32 fields require whole numbers within their type range; Decimal, Currency, VarNumeric, Single, and Double fields require numeric input. DateTime fields use Excel's native date validation and accept dates from `1900-01-01` through `9999-12-31`; earlier dates are unsupported. These rules use localized Stop-style error messages and respect nullable metadata. Int64/UInt64, Guid, binary, object, XML, and JSON values intentionally remain import-validated because Excel precision or reliable native validation is insufficient. Excel validation is an early-entry aid and can be bypassed by paste, macros, or external writers, so extraction and model validation remain authoritative.

Generated column widths follow simplex-property `DataType` and semantic `Role`. Character columns also use their declared `Length`, with practical minimum/default widths and a maximum width of 50. Properties whose role is `Currency` use Excel's locale-aware built-in currency format. Primary-key data columns are centered and use bold Maroon text. Center alignment for keys, enums, dates, Boolean values, identifiers and applicable semantic roles is also stored as the worksheet-column default, so values entered into rows added by resizing the Table retain the same alignment.

### Edited workbook recovery

Excel users sometimes append records below a table without expanding it. When the table does not use a totals row, the extractor keeps the table's column boundary but extends the last data row to the last non-empty cell below those columns. Empty rows are ignored. This recovers common edits without treating unrelated columns as model data.

Keep notes and unrelated content outside the table's column band: content below those columns can intentionally be interpreted as an appended record. When a totals row is enabled, only the table's declared data range is extracted.

Files that contain only a model-level Defined Name, an invalid reference, or merely a worksheet named after the model are intentionally rejected. Create an actual Excel Table whose name is the model name.

## Exporting Data

`SpreadsheetGenerator` creates a workbook and its model table in one operation:

```csharp
using Zongsoft.Data;
using Zongsoft.Externals.ClosedXml;

var model = Model.GetDescriptor<User>();
var users = GetUsers();

await using var output = File.Create("users.xlsx");
await new SpreadsheetGenerator().GenerateAsync(output, model, users);
```

Use `DataArchiveGeneratorOptions` to select exported fields:

```csharp
using Zongsoft.Data.Archiving;

var options = new DataArchiveGeneratorOptions(nameof(User.UserId), nameof(User.Name));
await generator.GenerateAsync(output, model, users, options);
```

Because the model name becomes an Excel Table name, it must satisfy Excel's table-name rules. The generator reports a localized validation error when it does not.

## Extracting Data

`SpreadsheetExtractor` obtains the model from the extraction options, locates the table named after that model, and maps its columns back to model properties:

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

To restrict lookup to a specific worksheet:

```csharp
var options = new DataArchiveExtractorOptions(model)
{
	Source = "Import",
};
```

The extractor reports a localized error when the worksheet, model table, or required model fields cannot be resolved.

## Zongsoft.Web Integration

The generator and extractor are registered as Zongsoft services for `IDataArchiveGenerator` and `IDataArchiveExtractor`. Once this extension is loaded by the application, a `ServiceController` import operation supplies its current model descriptor to the extractor. The default endpoint contract is therefore simple: upload a workbook containing an Excel Table whose name is the current model name.

No private generated table name is required, and a custom worksheet name does not change the model-table convention.

## Rendering Templates

`SpreadsheetRenderer` renders an `.xlsx` template using ClosedXML.Report variables. `SpreadsheetTemplateProvider` recursively discovers `.xlsx` files and indexes each template by its filename without the extension:

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

Template variables and expressions follow the [ClosedXML.Report](https://github.com/ClosedXML/ClosedXML.Report) syntax.

## Localization

English is the neutral resource language, and Simplified Chinese resources are provided for `zh-Hans`. Error messages follow `CultureInfo.CurrentUICulture`, so applications should establish the UI culture through their normal request or host localization pipeline.

## Sample

The interactive [sample project](samples/Program.cs) exports data, imports it again, and displays the workbook structure and extracted records for manual verification.

Run it from the repository root:

```shell
dotnet run --project externals/closedxml/samples/Zongsoft.Externals.ClosedXml.Samples.csproj -f net10.0
```

Available commands:

| Command | Description |
| --- | --- |
| `export [--culture:<name>\|-c:<name>] [file]` | Export sample users with an optional resource culture, then display the generated worksheet, table, range, columns, and rows. For example, `export -c:en-US users.xlsx`. |
| `import [file]` | Import a workbook and display its structure and extracted users. |
| `verify [file]` | Export and immediately import a workbook for an end-to-end check. |

The default file is `users.xlsx`. `out` and `in` are aliases for `export` and `import`.

For example, `export --culture:en-US users.en.xlsx` generates English titles and labels, while `export -c:zh-Hans users.zh-Hans.xlsx` generates Simplified Chinese ones. The selected culture applies only to that export command.

## Build and Test

```shell
dotnet build externals/closedxml/Zongsoft.Externals.ClosedXml.slnx --no-incremental
dotnet test externals/closedxml/test/Zongsoft.Externals.ClosedXml.Tests.csproj -f net10.0
```

## License

This project is licensed under the [GNU Lesser General Public License](../../LICENSE).

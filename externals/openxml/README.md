# Zongsoft.Externals.OpenXml Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.OpenXml)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.OpenXml)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**OpenXml**](https://github.com/Zongsoft/framework/tree/main/externals/openxml) provides a focused spreadsheet API on top of the [Open XML SDK](https://github.com/dotnet/Open-XML-SDK).

The `Zongsoft.Externals.OpenXml.Spreadsheet` namespace contains helpers for creating and opening workbooks, enumerating worksheets, addressing cells, and reading or updating cell values. `SpreadsheetDocument` accepts either a file path or a stream, so it can be used with local files as well as framework-provided storage.

Reference the package directly or load `Zongsoft.Externals.OpenXml.plugin`. See the [tests](test) for workbook creation, cell addressing, and spreadsheet access examples.

## Installation and Minimal Workbook

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

`SpreadsheetDocument.Create` accepts a path or writable stream and creates `Sheet1` when no names are supplied. `Open` accepts a path or stream and is read-only unless `editable: true` is requested. Dispose the wrapper to close the underlying Open XML package.

## Addressing and Values

Cell helpers use A1 addresses and ranges. They can set values, read text, convert values through `TryGetValue<T>`, and merge or unmerge a range. Sheets can be enumerated and added through `SheetCollection`.

> 💡 Open XML stores values, styles, shared strings, formulas, cached formula results, dates, and number formats separately. This focused API covers common cell value operations; use the underlying [Open XML SDK](https://learn.microsoft.com/office/open-xml/open-xml-sdk) when a workbook needs charts, advanced styles, recalculation metadata, macros, or other package parts.

## Limits and Safety

The package does not launch Excel and does not calculate formulas. Streams must remain seekable/readable/writable as required by the Open XML SDK. Large worksheets are document trees in memory unless the caller adopts lower-level streaming APIs.

🚨 Treat uploaded workbooks as untrusted ZIP packages. Bound file and expanded sizes, reject unexpected external relationships or macros in security-sensitive workflows, sanitize output paths, and do not overwrite a source file without a recoverable copy.

## Related Resources

- [Workbook tests](test)
- [SpreadsheetML overview](https://learn.microsoft.com/office/open-xml/spreadsheet/overview)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The plugin loads the workbook adapter assembly; application code still opens and disposes each workbook explicitly. It does not create an automatic spreadsheet conversion service or calculate formulas.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.OpenXml` | [Zongsoft.Externals.OpenXml.plugin](src/Zongsoft.Externals.OpenXml.plugin) |
| File copying and dependencies | [Zongsoft.Externals.OpenXml.deploy](src/Zongsoft.Externals.OpenXml.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals openxml]
nuget:Zongsoft.Externals.OpenXml
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.OpenXml.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

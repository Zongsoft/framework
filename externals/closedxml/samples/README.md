# Zongsoft.Externals.ClosedXml Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This sample demonstrates spreadsheet import and export with `Zongsoft.Externals.ClosedXml`. It generates localized `User` records with Bogus, exports them through `SpreadsheetGenerator`, imports them through `SpreadsheetExtractor`, and displays the resulting workbook metadata and data.

## Run

The sample targets .NET 10. Run it from the repository root:

```shell
dotnet run --project externals/closedxml/samples/Zongsoft.Externals.ClosedXml.Samples.csproj
```

## Export

`export` creates localized fake users and writes a formatted workbook. `out` is an alias. `--count` or `-c` controls the record count; `--culture` or `-l` selects data and spreadsheet localization:

```text
export --count:20 --culture:zh-CN users.xlsx
out -c:50 -l:en-US users.en-US.xlsx
```

If no output file is specified, the sample creates `users.<culture>(<count>).xlsx` in the current directory. After export it displays the worksheet, table, range, column count, and declared row count.

## Import

`import` reads workbook rows through `SpreadsheetExtractor`; `in` is an alias:

```text
import users.xlsx
in users.en-US.xlsx
```

If no file is specified, the command reads `users.xlsx` from the current directory. The workbook metadata and every imported `User` record are printed.

## Verify a Round Trip

`verify` exports a workbook and immediately imports the same file, making it useful for checking type conversion, nullable values, localization, and long text:

```text
verify --count:10 --culture:en-US
verify -c:25 -l:zh-CN roundtrip.xlsx
```

Confirm that the reported imported record count matches `--count` and inspect the displayed values for the selected culture.

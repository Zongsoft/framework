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

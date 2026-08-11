# Zongsoft.Data.DuckDB Data Driver Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.DuckDB)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.DuckDB)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**ata.**D**uckDB](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/duckdb) is a low-level data engine driver in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides access to [_**D**uckDB_](https://duckdb.org) and is transparent to upper-layer applications; deploy this plugin library to the application plugin directory to enable it.

## Importer Implementation

The DuckDB importer uses two writing strategies:

1. For regular data types, it uses the [DuckDB.NET Appender](https://duckdb.net/docs/standard-appender.html) for bulk loading. The standard Appender requires values to match all physical table columns exactly in count, order, and type, while the Zongsoft data engine supports importing any selected subset of fields. The importer therefore creates a connection-local temporary table containing only the selected fields, bulk-appends the input rows to it, and then executes one `INSERT ... SELECT` statement to write them to the target table. This preserves field mapping and target-table defaults; when `IDataImportOptions.ConstraintIgnored` is enabled, the final statement uses `INSERT OR IGNORE`.
2. For database-specific custom types represented by `DbType.Object`, the concrete DuckDB type cannot be determined safely at runtime for the Appender. The importer falls back to parameterized row-by-row `INSERT` statements so that DuckDB.NET can perform its normal parameter binding and conversion.

Both strategies enlist in the ambient Zongsoft data transaction by default. They use an independent connection when `IDataImportOptions.TransactionSuppressed` is true, when no ambient transaction exists, or when the driver does not support transactions. An independent connection uses an internal transaction to keep the current import batch atomic and roll a failed batch back completely.

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

## When to Use DuckDB

DuckDB is an embedded analytical database: the application loads its engine in-process instead of connecting to a separate database server. It is suited to local analytical queries and imports. It is not interchangeable with a multi-process transactional server merely because it accepts SQL. The driver implements the [Zongsoft.Data mapping and Schema workflow](../../README.md).

## Install and Configure

```shell
dotnet add package Zongsoft.Data.DuckDB
```

The package uses DuckDB.NET.Data.Full, including native runtime assets. Publish for a supported OS/architecture and retain the native dependencies. Load the Data and DuckDB plugins, then select the exact driver name:

```xml
<option path="/Data">
	<connectionSettings>
		<connectionSetting connectionSetting.name="Analytics" driver="DuckDB"
			value="DataSource=analytics.duckdb" />
	</connectionSettings>
</option>
```

A file database persists across connections; `DataSource=:memory:` is useful for a short-lived experiment but is not a persistent store.

## Application Integration

The consumer obtains `IServiceProvider<IDataAccess>` through `ApplicationContext.Current.Services` or the application's `Module.Current.Services`, then selects the `Analytics` accessor. The engine chooses DuckDB from connection settings; business modules depend only on shared contracts.

See the [complete Data plugin workflow](../../README.md#plugin-quickstart) for manifests, connection settings, mapping and a real query. For DuckDB, change both the connection driver and command script driver to `DuckDB` and use the connection above. Do not move the standalone low-level connection pattern into business services.

## Minimal Connection Check

The following isolated example checks the driver's connection conversion and native runtime without creating application tables. Application CRUD should use IDataAccess and mapping, as described in the parent guide.

```csharp
using Zongsoft.Data.DuckDB;

await using var connection = DuckDBDriver.Instance.CreateConnection("DataSource=:memory:");
await connection.OpenAsync();
await using var command = connection.CreateCommand();
command.CommandText = "SELECT 42";
Console.WriteLine(await command.ExecuteScalarAsync());
```

The result is 42. This low-level example deliberately owns its connection; engine-managed operations instead own sessions and transactions.

## Limits and Troubleshooting

- Imported field names must match mappings and actual column types; defaults apply only to omitted target columns.
- The connection adapter delegates transaction creation to DuckDB without an explicit ADO.NET isolation level; do not rely on requesting different isolation modes.
- Complex/custom data may use the slower parameterized import path described above. Measure representative batches.
- A native-library load failure usually requires checking runtime assets and architecture, not changing SQL.
- File access, database locking and native buffers must be released before backup or replacement.

🚨 Back up file databases before schema changes. This driver does not automatically migrate existing tables, and constraint-ignoring imports can skip conflicting rows.

## Further Reading

- [Driver tests](test) include import, transactions, CRUD, RETURNING and connection settings.
- [DuckDB concurrency](https://duckdb.org/docs/stable/connect/concurrency)
- [Data implementation skill](../../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

The manifest adds the driver to `/Workbench/Data/Drivers` and the connection-settings registry. Configure the exact driver key shown above, deploy the application's mapping, then obtain IDataAccess from the host. Database creation/migration remains an explicit application operation.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Data.DuckDB` | [Zongsoft.Data.DuckDB.plugin](src/Zongsoft.Data.DuckDB.plugin) |
| File copying and dependencies | [Zongsoft.Data.DuckDB.deploy](src/Zongsoft.Data.DuckDB.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data duckdb]
nuget:Zongsoft.Data.DuckDB
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Data.DuckDB.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

# Zongsoft.Data.MySql Data Driver Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MySql)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.MySql)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**ata.**M**ySql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mysql) is a low-level data engine driver in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides access to [_**M**y**SQL**_](https://www.mysql.com) and is transparent to upper-layer applications; deploy this plugin library to the application plugin directory to enable it.


## When to Use This Driver

Choose this package when a Zongsoft data model is stored in MySQL. The public application contract remains `IDataAccess`/`IDataService<T>`; this package translates expressions and mutations into the database dialect and creates connections through MySqlConnector.

The dialect owns MySQL quoting, generated-value retrieval, paging, parameters, and bulk import. Confirm server SQL modes and character sets because they can change validation and comparison behavior.

## Installation and Plugin Deployment

```shell
dotnet add package Zongsoft.Data.MySql
```

Deploy `Zongsoft.Data.MySql.plugin` with the assembly. Its manifest registers the connection-settings parser and data driver under the framework workbench. A NuGet reference without the plugin manifest does not activate the driver in a plugin host.

## Connection Configuration

```xml
<options>
	<option path="/Data">
		<connectionSettings default="Application">
			<connectionSetting connectionSetting.name="Application"
			                   driver="MySql"
			                   value="Server=127.0.0.1;Port=3306;Database=docs;UserName=docs_app;Password=REPLACE_WITH_TEST_PASSWORD;Charset=utf8mb4" />
		</connectionSettings>
	</option>
</options>
```

The `driver` value is case-insensitive but should use the canonical `MySql` spelling. The `value` follows the [Core settings contract](../../../Zongsoft.Core/docs/settings.md): it contains explicit driver parameters and extension properties. `GetOptions()` maps driver parameters to the native options consumed by MySqlConnector; extension properties remain in the framework settings and its complete `Value`. For native provider capabilities, consult the [provider documentation](https://mysqlconnector.net/) for supported keywords.

> 💡 Keep entity mappings, criteria, paging, transactions, and `IDataAccess` usage in the shared [Zongsoft.Data guide](../../README.md). Switching drivers should not require application services to depend on provider connection classes.

The connection string uses [this driver's public settings model](src/Configuration/MySqlConnectionSettings.cs), which does not necessarily expose every underlying SDK keyword. Examples target local test environments only. Prepare databases, accounts and permissions, and replace explicitly marked test credentials; this is not a server initialization script.

## Access Through Application Contracts

The consumer module references Core; the host deploys Data and this driver. After startup, obtain the accessor matching the configured connection name:

```csharp
using Zongsoft.Data;
using Zongsoft.Services;

var provider = ApplicationContext.Current.Services
	.ResolveRequired<Zongsoft.Services.IServiceProvider<IDataAccess>>();
var data = provider.GetService("Application")
	?? throw new InvalidOperationException("Data accessor not found.");
Console.WriteLine(data.Name);
```

See the [Data plugin example](../../README.md#plugin-quickstart) for host configuration and named accessors. Prepare tables and field types for the selected database, and use the operations supported by this driver. Application services access data through the public contracts.

## Runtime Behavior

Use the asynchronous methods on `IDataAccess` / `IDataService<T>` and consume query results with `await foreach`. Await transaction completion, rollback, and resource disposal. Imports accept in-memory collections and asynchronous sources, and operations propagate cancellation tokens.

🚨 Never commit production connection strings. Use deployment-time configuration or a secret provider, grant the database identity only the required permissions, and verify destructive mutations against a disposable database first.

<a id="returning-values"></a>
## Returning Values

`DataUpdateOptions.Return` supports `Newer` and `Older`; `DataDeleteOptions.Return` returns the deleted values. The mapped entity must have a primary key, including every component of a composite key. Primary-key changes, including trigger-driven changes, are unsupported when requesting returned values. All participating tables must use InnoDB, transactions must be enabled, and the connection identity needs metadata visibility and `CREATE TEMPORARY TABLES` permission. Unsupported keys, suppressed transactions, views, and nontransactional tables are rejected.

The returned rows belong to the targets selected before the mutation, even when updated values no longer satisfy the original condition. Newer values are read after the update, including trigger changes. Results follow the [shared returning contract](../../README.md#usage-returning); affected-row counts retain MySqlConnector's configured matched/changed-row behavior. Insert/upsert do not provide general returning projections through this emulation.

## Compatibility and Testing

`ReturningSafetyTest` includes trigger scenarios and requires permission to create triggers. When binary logging is enabled, non-administrative trigger creation must also be allowed; the supplied test configuration sets `log_bin_trust_function_creators=1`.

Database behavior is not completely portable. Verify identifier casing, null and comparison rules, date/time precision, generated values, transaction semantics, and bulk-import constraints on the same server/provider versions used in production. Database integration tests require an explicitly configured disposable database. The `ConnectionSettingsTest` and `ConnectionSettingsPropertiesTest` classes run offline and cover native option conversion, extension properties, and complete settings text. Use a test filter for these classes when no database is available.

## Related Resources

- [Zongsoft.Data user guide](../../README.md)
- [Driver implementation rules](../../AGENTS.md)
- [Data engine implementation skill](../../SKILL.md)
- [MySQL provider documentation](https://mysqlconnector.net/)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

The manifest adds the driver to `/Workbench/Data/Drivers` and the connection-settings registry. Configure the exact driver key shown above, deploy the application's mapping, then obtain IDataAccess from the host. Database creation/migration remains an explicit application operation.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Data.MySql` | [Zongsoft.Data.MySql.plugin](src/Zongsoft.Data.MySql.plugin) |
| File copying and dependencies | [Zongsoft.Data.MySql.deploy](src/Zongsoft.Data.MySql.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data mysql]
nuget:Zongsoft.Data.MySql
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Data.MySql.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

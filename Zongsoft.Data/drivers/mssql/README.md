# Zongsoft.Data.MsSql Data Driver Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.MsSql)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.MsSql)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**ata.**M**s**S**ql](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/mssql) is a low-level data engine driver in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides access to [_**M**icrosoft **SQL** **S**erver_](https://www.microsoft.com/sql-server) and is transparent to upper-layer applications; deploy this plugin library to the application plugin directory to enable it.


## When to Use This Driver

Choose this package when a Zongsoft data model is stored in Microsoft SQL Server. The public application contract remains `IDataAccess`/`IDataService<T>`; this package translates expressions and mutations into the database dialect and creates connections through Microsoft.Data.SqlClient.

SQL Server quoting, identity retrieval, paging, parameters, and bulk import are emitted by this dialect. Server permissions and transaction isolation remain deployment concerns.

## Installation and Plugin Deployment

```shell
dotnet add package Zongsoft.Data.MsSql
```

Deploy `Zongsoft.Data.MsSql.plugin` with the assembly. Its manifest registers the connection-settings parser and data driver under the framework workbench. A NuGet reference without the plugin manifest does not activate the driver in a plugin host.

## Connection Configuration

```xml
<options>
	<option path="/Data">
		<connectionSettings default="Application">
			<connectionSetting connectionSetting.name="Application"
			                   driver="MsSql"
			                   value="Server=localhost;Database=docs;IntegratedSecurity=true" />
		</connectionSettings>
	</option>
</options>
```

The `driver` value is case-insensitive but should use the canonical `MsSql` spelling. The `value` is parsed by this package's connection settings driver and ultimately passed to Microsoft.Data.SqlClient; consult the [provider documentation](https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace) for supported keywords.

> 💡 Keep entity mappings, criteria, paging, transactions, and `IDataAccess` usage in the shared [Zongsoft.Data guide](../../README.md). Switching drivers should not require application services to depend on provider connection classes.

The connection string uses [this driver's public settings model](src/Configuration/MsSqlConnectionSettings.cs), which does not necessarily expose every underlying SDK keyword. Examples target local test environments only. Prepare databases, accounts and permissions, and replace explicitly marked test credentials; this is not a server initialization script.

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

Obtaining an accessor does not prove that a database connection or SQL execution succeeded. Next, invoke a query from a deployed mapping. See the [complete Data plugin workflow](../../README.md#plugin-quickstart) for manifests, connection settings, mapping, a named command and its result. That example uses SQLite; for this driver, retain the interface pattern and replace the connection and dialect-specific script. Business modules need not construct database connections; a module can use `Module.Current.Services`.

## Runtime Behavior

The driver supplies a provider-specific statement builder/visitor, command parameterization, execution primitives, and an importer. The data engine selects it from the named connection setting. Ambient data transactions are honored where the provider and operation support them.

🚨 Never commit production connection strings. Use deployment-time configuration or a secret provider, grant the database identity only the required permissions, and verify destructive mutations against a disposable database first.

## Compatibility and Testing

Database behavior is not completely portable. Verify identifier casing, null and comparison rules, date/time precision, generated values, transaction semantics, and bulk-import constraints on the same server/provider versions used in production. Repository tests require an explicitly configured disposable database and are not ordinary offline unit tests.

## Related Resources

- [Zongsoft.Data user guide](../../README.md)
- [Driver implementation rules](../../AGENTS.md)
- [Data engine implementation skill](../../SKILL.md)
- [Microsoft SQL Server provider documentation](https://learn.microsoft.com/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

The manifest adds the driver to `/Workbench/Data/Drivers` and the connection-settings registry. Configure the exact driver key shown above, deploy the application's mapping, then obtain IDataAccess from the host. Database creation/migration remains an explicit application operation.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Data.MsSql` | [Zongsoft.Data.MsSql.plugin](src/Zongsoft.Data.MsSql.plugin) |
| File copying and dependencies | [Zongsoft.Data.MsSql.deploy](src/Zongsoft.Data.MsSql.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data mssql]
nuget:Zongsoft.Data.MsSql
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Data.MsSql.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

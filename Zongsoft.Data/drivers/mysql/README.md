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

The `driver` value is case-insensitive but should use the canonical `MySql` spelling. The `value` is parsed by this package's connection settings driver and ultimately passed to MySqlConnector; consult the [provider documentation](https://mysqlconnector.net/) for supported keywords.

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

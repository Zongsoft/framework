# Zongsoft.Data.TDengine Data Driver Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.TDengine)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**D**ata.**TD**engine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) is a low-level data engine driver in the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework. It provides access to [_**TD**engine_](https://www.taosdata.com) and is transparent to upper-layer applications; deploy this plugin library to the application plugin directory to enable it.


## When to Use This Driver

Choose this package when a Zongsoft data model is stored in TDengine. The public application contract remains `IDataAccess`/`IDataService<T>`; this driver accesses TDengine over WebSocket.

This driver supports WebSocket only, using the taosAdapter port 6041 by default. Explicit Native configuration is rejected. Super tables, tags, timestamp precision, and server compatibility remain part of the data contract.

## Installation and Plugin Deployment

```shell
dotnet add package Zongsoft.Data.TDengine
```

Deploy `Zongsoft.Data.TDengine.plugin` with the assembly. Its manifest registers the connection-settings parser and data driver under the framework workbench. A NuGet reference without the plugin manifest does not activate the driver in a plugin host.

## Connection Configuration

```xml
<options>
	<option path="/Data">
		<connectionSettings default="Application">
			<connectionSetting connectionSetting.name="Application"
			                   driver="TDengine"
			                   value="Server=127.0.0.1;Port=6041;Database=docs;UserName=docs_app;Password=REPLACE_WITH_TEST_PASSWORD;Protocol=WebSocket" />
		</connectionSettings>
	</option>
</options>
```

The `driver` value is case-insensitive but should use the canonical `TDengine` spelling. The `value` follows the [Core settings contract](../../../Zongsoft.Core/docs/settings.md): it contains explicit driver parameters and extension properties. Use the settings exposed by this driver and ensure its taosAdapter WebSocket endpoint is reachable.

> 💡 Keep entity mappings, criteria, paging, transactions, and `IDataAccess` usage in the shared [Zongsoft.Data guide](../../README.md). Switching drivers should not require application services to depend on provider connection classes.

The connection string uses [this driver's public settings model](src/Configuration/TDengineConnectionSettings.cs), which does not necessarily expose every underlying SDK keyword. Examples target local test environments only. Prepare databases, accounts and permissions, and replace explicitly marked test credentials; this is not a server initialization script.

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

Use the asynchronous methods on `IDataAccess` / `IDataService<T>` and consume query results with `await foreach`. Await transaction completion, rollback, and resource disposal. Imports accept in-memory collections and asynchronous sources, and operations propagate cancellation tokens. This driver does not support database transactions; ambient transactions cannot roll back TDengine writes.

🚨 Never commit production connection strings. Use deployment-time configuration or a secret provider, grant the database identity only the required permissions, and verify destructive mutations against a disposable database first.

## Compatibility and Testing

The connector dependency is the community-maintained [TDengine.Connector.Async 3.2.1.7](https://www.nuget.org/packages/TDengine.Connector.Async/3.2.1.7). Deploy the dependency version matching this driver.

Database behavior is not completely portable. Verify identifier casing, null and comparison rules, date/time precision, generated values, transaction semantics, and bulk-import constraints on the same server/provider versions used in production. Repository tests require an explicitly configured disposable database and are not ordinary offline unit tests.

## Related Resources

- [Zongsoft.Data user guide](../../README.md)
- [Driver implementation rules](../../AGENTS.md)
- [Data engine implementation skill](../../SKILL.md)
- [TDengine provider documentation](https://docs.tdengine.com/developer-guide/connectors-reference/csharp/)
## Unit Testing

> [!IMPORTANT]
> Database tests require a reachable TDengine server and taosAdapter WebSocket endpoint. A local taosc client is not required. Prepare database accounts, permissions, and mappings before testing.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../Zongsoft.Plugins/README.md).

The manifest adds the driver to `/Workbench/Data/Drivers` and the connection-settings registry. Configure the exact driver key shown above, deploy the application's mapping, then obtain IDataAccess from the host. Database creation/migration remains an explicit application operation.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Data.TDengine` | [Zongsoft.Data.TDengine.plugin](src/Zongsoft.Data.TDengine.plugin) |
| File copying and dependencies | [Zongsoft.Data.TDengine.deploy](src/Zongsoft.Data.TDengine.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data tdengine]
nuget:Zongsoft.Data.TDengine
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Data.TDengine.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

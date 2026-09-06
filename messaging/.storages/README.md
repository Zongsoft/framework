# Zongsoft.Messaging.Storages.Data

[简体中文](README.zh-Hans.md)

`Zongsoft.Messaging.Storages.Data` stores reliable Zongsoft messages through `IDataAccess` and preloaded `DataCommand` definitions. It supports SQLite, MySQL, PostgreSQL, and SQL Server without referencing their ADO.NET packages directly. A shared `Zongsoft.Messaging.Storages.mapping` defines the commands and parameters; driver-specific SQL is loaded from qualified-name files under `scripts`. A driver subdirectory supplies the driver name, so its files are named like `mysql/Messaging.Storages.Get.sql`. When scripts are placed together without driver subdirectories, their names must include the driver suffix, such as `Messaging.Storages.Get-mysql.sql`. The C# hot path contains no embedded SQL text.

## Configuration

Install `Zongsoft.Data`, this plugin, and the selected data-driver plugin. Create `Messaging_Message` with the matching script under `database`, then configure a connection whose name is exactly the Broker name. The daemon-created ZeroMQ Broker is named `QueueServer`.

Set a stable storage identifier before the process starts:

```powershell
$env:ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER = "broker-storage-01"
```

```xml
<option path="/Data">
	<connectionSettings>
		<connectionSetting connectionSetting.name="QueueServer" driver="SQLite"
		                   value="DataSource=broker.db;PRAGMA:journal_mode=WAL;" />
	</connectionSettings>
</option>
```

Inject the selected factory through its unified plugin path:

```xml
<extension path="/Workbench/Messaging/Zero">
	<QueueServer.Storages>{path:/Workspace/Messaging/Storages/Sqlite}</QueueServer.Storages>
</extension>
```

The four factory paths end in `Sqlite`, `MySql`, `PostgreSql`, and `MsSql`. Each factory first performs an exact same-name lookup under `/Data/ConnectionSettings`, then under `/Messaging/Storages/ConnectionSettings`; there is no default-connection fallback. A factory freezes `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` on first use and falls back to `Environment.MachineName` when the variable is empty. The storage partition is `Zongsoft.Messaging.Storage:{ConnectionSettings.Name}:{StorageIdentifier}`; partitions over 128 characters use a stable SHA-256 form.

When upgrading from the former `nodeId` option, set `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` to the same value before the factories are first used. The partition text remains compatible when the value is unchanged; omitting it may select the machine-name partition and leave previous reliable messages in the old database namespace.

The storage snapshots all message metadata and payload bytes, uses native UTC expiration filtering, and never disposes the shared `IDataAccess`. Expired rows are not removed in the background; schedule an operational cleanup if physical reclamation is required.

## Reliability Concepts and Scope

A stored message is an independent snapshot of payload and metadata, not a live consumer or acknowledgment delegate. This storage supports a broker's reliable-delivery bookkeeping; it does not make the business database update and message delivery one atomic transaction.

A **partition** separates connection name and stable storage identity. A **topic** filters messages inside it. Changing either identity after deployment can make old records invisible even though the rows still exist.

## Install and Start Safely

```shell
dotnet add package Zongsoft.Messaging.Storages.Data
dotnet add package Zongsoft.Data.SQLite
```

For the SQLite configuration above, initialize a disposable `broker.db` using [the SQLite schema](database/sqlite/README.md) before starting the broker. Deploy the mapping and all selected-driver command scripts as listed by the plugin's deployment manifest; copying only the DLL is insufficient.

The complete configuration fragments above assume a ZeroMQ broker host already exists; see [the ZeroMQ guide](../zero/README.md) for that host and message workflow. Select the storage factory during startup and keep the storage identity unchanged across restarts.

## Message Lifecycle and Operations

- Set creates or replaces all persisted fields for the identifier in the partition; replacing a record can clear its previous expiration.
- Get filters expired rows and matches topic names ordinally; it does not reconstruct a broker acknowledgment callback.
- Remove reports whether a record existed. Clear affects the selected topic or partition, not unrelated partitions.
- Disposing storage does not dispose the shared IDataAccess. The host owns that service.
- Expiration filtering does not reclaim rows. Implement a bounded, partition-aware maintenance policy and monitor storage growth.

🚨 Database backup and namespace stability matter to reliable delivery. Deleting rows, changing identity or using a mismatched schema can lose pending messages. A successful write is not proof that downstream business processing occurred exactly once.

## Troubleshooting and Tests

Missing same-name connection, absent mapping commands, unavailable driver plugin and wrong script deployment are distinct startup failures. Verify each before changing SQL. Use [contract tests](test/README.md) for snapshots, ordinal topics, replacement, cancellation and partition isolation.

Schemas: [SQLite](database/sqlite/README.md), [MySQL](database/mysql/README.md), [PostgreSQL](database/postgresql/README.md), [SQL Server](database/mssql/README.md).

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Add the selected Data driver, initialize its schema, configure the exact broker connection name and select `/Workspace/Messaging/Storages/{factory}`. Deploy `scripts` and mapping together; the factory does not provision a broker or database.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Messaging.Storages.Data` | [Zongsoft.Messaging.Storages.Data.plugin](src/Zongsoft.Messaging.Storages.Data.plugin) |
| File copying and dependencies | [Zongsoft.Messaging.Storages.Data.deploy](src/Zongsoft.Messaging.Storages.Data.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft messaging storages data]
nuget:Zongsoft.Messaging.Storages.Data
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Messaging.Storages.Data.option`, `Zongsoft.Messaging.Storages.Data.plugin`, `Zongsoft.Messaging.Storages.mapping`, `scripts/**/*`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

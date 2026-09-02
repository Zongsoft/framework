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

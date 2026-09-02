# Zongsoft.Messaging.Storages.Data 数据库消息存储

[English](README.md)

`Zongsoft.Messaging.Storages.Data` 通过 `IDataAccess` 和预加载的 `DataCommand` 定义保存可靠消息。它支持 SQLite、MySQL、PostgreSQL、SQL Server，程序集本身不直接引用这些数据库的 ADO.NET 包。公共的 `Zongsoft.Messaging.Storages.mapping` 定义命令和参数，各驱动 SQL 则通过 `scripts` 下以命令限定名命名的文件加载。驱动子目录本身提供驱动名，因此其中的文件采用 `mysql/Messaging.Storages.Get.sql` 形式；如果没有驱动子目录、所有脚本平铺在一起，文件名必须采用 `Messaging.Storages.Get-mysql.sql` 形式携带驱动后缀。C# 热路径中不嵌入 SQL 文本。

## 配置

安装 `Zongsoft.Data`、本插件和所选数据库驱动插件。先执行 `database` 下对应的脚本创建 `Messaging_Message`，再配置与 Broker 严格同名的数据连接。守护插件创建的 ZeroMQ Broker 名为 `QueueServer`。

在进程启动前设置稳定的存储标识：

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

通过统一插件路径为消息队列服务器注入所选工厂：

```xml
<extension path="/Workbench/Messaging/Zero">
	<QueueServer.Storages>{path:/Workspace/Messaging/Storages/Sqlite}</QueueServer.Storages>
</extension>
```

四个工厂路径末段分别为 `Sqlite`、`MySql`、`PostgreSql`、`MsSql`。工厂先在 `/Data/ConnectionSettings` 中精确查找同名连接，再查找 `/Messaging/Storages/ConnectionSettings`，不回退默认连接。工厂首次使用时冻结 `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` 环境变量；该变量为空时回退到 `Environment.MachineName`。数据分区为 `Zongsoft.Messaging.Storage:{ConnectionSettings.Name}:{StorageIdentifier}`，分区原文超过128字符时使用稳定的 SHA-256 形式。

从旧版 `nodeId` 选项升级时，必须在工厂首次使用前将相同值设置到 `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER`。值不变时分区文本保持兼容；若未设置，系统可能改用机器名分区，使旧可靠消息留在原数据库命名空间中。

存储器会快照全部消息元数据和负载字节，通过数据库按 UTC 过期时间过滤，并且不会释放共享的 `IDataAccess`。过期行不会在后台自动删除；如需回收物理空间，请配置运维清理任务。

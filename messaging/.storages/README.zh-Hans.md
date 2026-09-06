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

## 可靠性概念与范围

存储的消息是载荷和元数据的独立快照，不是活的消费者或确认委托。本存储支撑 Broker 的可靠投递记账，但不会把业务数据库更新和消息投递自动变成同一个原子事务。

**分区**由连接名称与稳定存储身份隔离；**主题**在分区内筛选消息。部署后改变任一身份，都可能让旧记录不可见，即使数据行仍然存在。

## 安装与安全启动

```shell
dotnet add package Zongsoft.Messaging.Storages.Data
dotnet add package Zongsoft.Data.SQLite
```

使用上面的 SQLite 配置时，先按 [SQLite 建表说明](database/sqlite/README.zh-Hans.md)初始化临时 `broker.db`，再启动 Broker。按插件部署清单保留映射及所选驱动的命令脚本，只复制 DLL 不足以工作。

前面的完整配置片段假设已存在 ZeroMQ Broker 宿主；宿主及消息工作流见 [ZeroMQ 指南](../zero/README.zh-Hans.md)。启动时选择存储工厂，跨重启保持存储身份不变。

## 消息生命周期与操作

- Set 在分区内按标识创建或替换全部持久字段，替换可清除之前的过期时间。
- Get 过滤过期行并按序数精确匹配主题，不会重建 Broker 确认回调。
- Remove 返回记录是否存在；Clear 只影响选定主题或分区，不影响其它分区。
- 释放存储不会释放共享 IDataAccess，该服务由宿主拥有。
- 过期筛选不会回收行，应实施有界、按分区限定的维护策略并监控增长。

🚨 数据库备份与命名空间稳定性关系到可靠投递。删除行、改变身份或使用不匹配的表结构都可能丢失待投递消息。写入成功不代表下游业务恰好执行了一次。

## 排障与测试

缺少同名连接、映射命令缺失、驱动插件不可用及脚本部署错误是不同故障，应逐项确认后再修改 SQL。[契约测试](test/README.zh-Hans.md)覆盖快照、主题序数匹配、替换、取消及分区隔离。

建表说明：[SQLite](database/sqlite/README.zh-Hans.md)、[MySQL](database/mysql/README.zh-Hans.md)、[PostgreSQL](database/postgresql/README.zh-Hans.md)、[SQL Server](database/mssql/README.zh-Hans.md)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

增加选定的 Data 驱动、初始化其表结构、配置与 Broker 完全同名的连接，再选择 `/Workspace/Messaging/Storages/{factory}`。脚本与映射必须一起部署，工厂不会创建 Broker 或数据库。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Messaging.Storages.Data` | [Zongsoft.Messaging.Storages.Data.plugin](src/Zongsoft.Messaging.Storages.Data.plugin) |
| 文件复制及依赖 | [Zongsoft.Messaging.Storages.Data.deploy](src/Zongsoft.Messaging.Storages.Data.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft messaging storages data]
nuget:Zongsoft.Messaging.Storages.Data
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Messaging.Storages.Data.option`、`Zongsoft.Messaging.Storages.Data.plugin`、`Zongsoft.Messaging.Storages.mapping`、`scripts/**/*`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

# Zongsoft.Data.DuckDB 数据驱动插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.DuckDB)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.DuckDB)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**ata.**D**uckDB](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/duckdb) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的数据引擎的底层驱动，提供了 [_**D**uckDB_](https://duckdb.org) 数据库访问的相关功能，对上层应用透明，只需将该插件库部署到应用程序的插件目录中即可。

## 导入器实现

DuckDB 导入器根据数据类型采用两种写入方式：

1. 对于常规数据类型，使用 [DuckDB.NET Appender](https://duckdb.net/docs/standard-appender.html) 进行批量写入。标准 Appender 要求写入值与目标表的全部物理列在数量、顺序和类型上完全一致，而 Zongsoft 数据引擎允许只导入任意字段子集。因此，导入器先创建一个仅包含导入字段的连接级临时表，通过 Appender 将数据批量写入该表，再以一条 `INSERT ... SELECT` 语句写入目标表。这既保留了字段映射和目标表默认值，也能在启用 `IDataImportOptions.ConstraintIgnored` 时通过 `INSERT OR IGNORE` 忽略约束冲突。
2. 对于以 `DbType.Object` 表示的数据库自定义类型，运行时无法为 Appender 安全确定具体的 DuckDB 类型，因此回退为参数化的逐行 `INSERT`，由 DuckDB.NET 完成常规的参数绑定和类型转换。

两种方式默认加入 Zongsoft 的环境事务；当 `IDataImportOptions.TransactionSuppressed` 为真、当前没有环境事务或驱动不支持事务时，则使用独立连接。独立连接使用内部事务保证当前导入批次的原子性，确保失败时完整回滚。

## 何时选择 DuckDB

DuckDB 是嵌入式分析数据库：引擎加载在应用进程中，而不是连接独立数据库服务器，适合本地分析查询与数据导入。它支持 SQL，但不能因此视为多进程事务服务器的直接替代品。本驱动实现 [Zongsoft.Data 的映射与 Schema 工作流](../../README.zh-Hans.md)。

## 安装与配置

```shell
dotnet add package Zongsoft.Data.DuckDB
```

本包使用 DuckDB.NET.Data.Full，包含原生运行时资源。应面向支持的系统/架构发布并保留原生依赖。加载 Data 与 DuckDB 插件后选择准确驱动名称：

```xml
<option path="/Data">
	<connectionSettings>
		<connectionSetting connectionSetting.name="Analytics" driver="DuckDB"
			value="DataSource=analytics.duckdb" />
	</connectionSettings>
</option>
```

文件数据库可跨连接持久保存；`DataSource=:memory:` 适合短期实验，不是持久存储。

## 应用层接入

消费模块通过 `ApplicationContext.Current.Services` 或应用定义的 `Module.Current.Services` 取得 `IServiceProvider<IDataAccess>`，再按连接名 `Analytics` 获取访问器。引擎根据连接配置选择 DuckDB，业务模块只依赖公共契约。

包含清单、连接、映射与实际查询的完整例子见 [Data 插件最小闭环](../../README.zh-Hans.md#plugin-quickstart)。用于 DuckDB 时，把驱动键和命令脚本的 driver 改为 `DuckDB`，并使用上文的连接；不要把独立工具的底层连接写法搬进业务服务。

## 最小连接验证

以下独立示例验证驱动连接转换和原生运行时，不创建业务表。应用 CRUD 应按父级指南使用 IDataAccess 与映射。

```csharp
using Zongsoft.Data.DuckDB;

await using var connection = DuckDBDriver.Instance.CreateConnection("DataSource=:memory:");
await connection.OpenAsync();
await using var command = connection.CreateCommand();
command.CommandText = "SELECT 42";
Console.WriteLine(await command.ExecuteScalarAsync());
```

结果为 42。这个底层示例明确拥有连接；数据引擎操作则由会话与事务管理连接。

## 限制与排障

- 导入字段名必须与映射及实际列类型一致，默认值只应用于省略的目标列。
- 连接适配器让 DuckDB 自行创建事务，不传递显式 ADO.NET 隔离级别；不能依赖请求不同隔离模式。
- 复杂/自定义类型可能走前述较慢的逐行参数化导入路径，应测量有代表性的批次。
- 原生库加载失败通常应检查运行时资源与架构，而不是修改 SQL。
- 备份或替换文件前应释放文件访问、数据库锁与原生缓冲。

🚨 修改结构前先备份文件数据库。本驱动不会自动迁移既有表，忽略约束的导入可能跳过冲突行。

## 延伸阅读

- [驱动测试](test)涵盖导入、事务、CRUD、RETURNING 与连接设置。
- [DuckDB 并发模型](https://duckdb.org/docs/stable/connect/concurrency)
- [Data 实现技能](../../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

清单将驱动加入 `/Workbench/Data/Drivers` 及连接设置注册表。配置前文的准确驱动键、部署应用映射，再从宿主获取 IDataAccess。创建或迁移数据库仍是应用的显式操作。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Data.DuckDB` | [Zongsoft.Data.DuckDB.plugin](src/Zongsoft.Data.DuckDB.plugin) |
| 文件复制及依赖 | [Zongsoft.Data.DuckDB.deploy](src/Zongsoft.Data.DuckDB.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data duckdb]
nuget:Zongsoft.Data.DuckDB
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Data.DuckDB.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

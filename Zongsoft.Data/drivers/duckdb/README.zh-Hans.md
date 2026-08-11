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

# Zongsoft.Data.SQLite 数据驱动插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.SQLite)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.SQLite)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**ata.**SQL**ite](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/sqlite) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的数据引擎的底层驱动，提供了 [_**SQL**ite_](https://sqlite.org) 数据库访问的相关功能，对上层应用透明，只需将该插件库部署到应用程序的插件目录中即可。


## 适用场景

当 Zongsoft 数据模型存储于 SQLite 时选择本包。上层应用仍面向 `IDataAccess`/`IDataService<T>`；本包负责把表达式和数据变更翻译为对应方言，并通过 Microsoft.Data.Sqlite 创建连接。

SQLite 是嵌入式文件数据库，并发写入、锁、日志模式、内存连接生命周期、类型亲和性和路径权限与客户端/服务器数据库明显不同。

## 安装与插件部署

```shell
dotnet add package Zongsoft.Data.SQLite
```

请将 `Zongsoft.Data.SQLite.plugin` 与程序集一起部署。清单会在框架工作台中注册连接设置解析器和数据驱动；只有 NuGet 引用而缺少插件清单，不会在插件宿主中激活驱动。

## 连接配置

```xml
<options>
	<option path="/Data">
		<connectionSettings default="Application">
			<connectionSetting connectionSetting.name="Application"
			                   driver="SQLite"
			                   value="Database=docs.db;Mode=ReadWriteCreate;ForeignKeys=true" />
		</connectionSettings>
	</option>
</options>
```

`driver` 不区分大小写，但建议使用规范拼写 `SQLite`。`value` 由本包的连接设置驱动解析，并最终传给 Microsoft.Data.Sqlite；可用关键字请查阅[提供程序文档](https://learn.microsoft.com/zh-cn/dotnet/standard/data/sqlite/)。

> 💡 实体映射、条件、分页、事务和 `IDataAccess` 用法统一参阅 [Zongsoft.Data 指南](../../README.zh-Hans.md)。切换驱动不应迫使应用服务依赖提供程序连接类型。

连接字符串使用[本驱动公开的设置模型](src/Configuration/SQLiteConnectionSettings.cs)，不保证支持底层 SDK 的全部关键字。示例创建或打开本机相对路径的文件库，不需要服务器账号；运行身份需有文件及目录权限。`ReadWriteCreate` 允许创建缺失文件，`ReadWrite` 要求文件已存在；这不会自动创建业务表。

## 从应用接口访问

消费模块引用 Core；宿主负责部署 Data 与本驱动。启动后通过公共提供者取得与配置同名的访问器：

```csharp
using Zongsoft.Data;
using Zongsoft.Services;

var provider = ApplicationContext.Current.Services
	.ResolveRequired<Zongsoft.Services.IServiceProvider<IDataAccess>>();
var data = provider.GetService("Application")
	?? throw new InvalidOperationException("Data accessor not found.");
Console.WriteLine(data.Name);
```

取得访问器尚未证明数据库连接或 SQL 成功；下一步应调用已部署映射中的查询。完整的“清单、连接、映射、命名命令、实际结果”见 [Data 插件最小闭环](../../README.zh-Hans.md#plugin-quickstart)。该示例使用 SQLite；接入本驱动时保留接口用法，替换连接及适用方言脚本。业务模块不需要直接构造数据库连接，模块内可改用 `Module.Current.Services`。

## 运行行为

本驱动提供数据库专属的语句构建器/访问器、命令参数化、执行基元与导入器。数据引擎根据具名连接设置选择它；在提供程序和操作支持时，驱动会加入环境数据事务。

🚨 限制数据库文件及其目录的访问权限；备份前核对 WAL/日志文件与连接状态。修改结构或清空数据前先在可丢弃文件库中验证。

## 兼容性与测试

数据库行为无法完全移植。请在与生产相同的服务器/提供程序版本上验证标识符大小写、空值和比较规则、日期时间精度、生成值、事务语义与批量导入约束。仓库测试需要显式配置可丢弃数据库，不属于普通离线单元测试。

## 延伸阅读

- [Zongsoft.Data 使用指南](../../README.zh-Hans.md)
- [驱动实现规则](../../AGENTS.md)
- [数据引擎实现技能](../../SKILL.md)
- [SQLite 提供程序文档](https://learn.microsoft.com/zh-cn/dotnet/standard/data/sqlite/)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

清单将驱动加入 `/Workbench/Data/Drivers` 及连接设置注册表。配置前文的准确驱动键、部署应用映射，再从宿主获取 IDataAccess。创建或迁移数据库仍是应用的显式操作。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Data.SQLite` | [Zongsoft.Data.SQLite.plugin](src/Zongsoft.Data.SQLite.plugin) |
| 文件复制及依赖 | [Zongsoft.Data.SQLite.deploy](src/Zongsoft.Data.SQLite.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data sqlite]
nuget:Zongsoft.Data.SQLite
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Data.SQLite.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

### 原生依赖排障

Windows x64 的手工部署已验证需要让匹配架构的 `e_sqlite3.dll` 可被托管组件加载，不能假设插件子目录中的 `runtimes` 会自动加入原生搜索路径。详见 [Data 验证记录](../../README.zh-Hans.md#plugin-quickstart)。当前项目引用 Microsoft.Data.Sqlite `10.0.10`，部署清单仍指定 `10.0.7`；定向构建还报告 SQLitePCLRaw.lib.e_sqlite3 `2.1.11` 的 `NU1903` 安全警告。部署前应审核实际依赖和漏洞信息，不因本地常量查询通过就推断版本已适合生产。

# Zongsoft.Data.ClickHouse 数据驱动插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.ClickHouse)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.ClickHouse)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**ata.**C**lick**H**ouse](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/clickhouse) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的数据引擎的底层驱动，提供了 [_**C**lick**H**ouse_](https://clickhouse.com) 数据库访问的相关功能，对上层应用透明，只需将该插件库部署到应用程序的插件目录中即可。


## 适用场景

当 Zongsoft 数据模型存储于 ClickHouse 时选择本包。上层应用仍面向 `IDataAccess`/`IDataService<T>`；本包负责把表达式和数据变更翻译为对应方言，并通过 ClickHouse.Driver 创建连接。

ClickHouse 是面向分析负载的列式数据库。将原本针对 OLTP 数据库设计的数据服务模式用于它之前，应确认变更操作与事务预期。

## 安装与插件部署

```shell
dotnet add package Zongsoft.Data.ClickHouse
```

请将 `Zongsoft.Data.ClickHouse.plugin` 与程序集一起部署。清单会在框架工作台中注册连接设置解析器和数据驱动；只有 NuGet 引用而缺少插件清单，不会在插件宿主中激活驱动。

## 连接配置

```xml
<options>
	<option path="/Data">
		<connectionSettings default="Application">
			<connectionSetting connectionSetting.name="Application"
			                   driver="ClickHouse"
			                   value="Server=127.0.0.1;Port=8123;Database=docs;UserName=docs_app;Password=REPLACE_WITH_TEST_PASSWORD;Protocol=http" />
		</connectionSettings>
	</option>
</options>
```

`driver` 不区分大小写，但建议使用规范拼写 `ClickHouse`。`value` 遵循 [Core 设置契约](../../../Zongsoft.Core/docs/settings.zh-Hans.md)，包含显式驱动参数和扩展属性。`GetOptions()` 将驱动参数映射为 ClickHouse.Driver 使用的原生选项；扩展属性保留在框架连接设置（`ConnectionSettings`）及其完整 `Value` 中。提供程序自身能力请查阅[提供程序文档](https://clickhouse.com/docs/integrations/csharp)。

> 💡 实体映射、条件、分页、事务和 `IDataAccess` 用法统一参阅 [Zongsoft.Data 指南](../../README.zh-Hans.md)。切换驱动不应迫使应用服务依赖提供程序连接类型。

连接字符串使用[本驱动公开的设置模型](src/Configuration/ClickHouseConnectionSettings.cs)，不保证支持底层 SDK 的全部关键字。示例仅指向本机测试环境；需要先准备数据库、账号及权限，并替换明确标注的测试凭据。不要把示例当作服务器初始化脚本。

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

宿主装配与具名访问器的用法见 [Data 插件示例](../../README.zh-Hans.md#plugin-quickstart)。准备与所选数据库匹配的表结构和字段类型，并使用本驱动支持的操作；业务服务通过公共契约访问数据。

提供程序为 `ClickHouse.Driver` 1.4.0。`Compression` 设置同时控制批量写入压缩；没有 `UseServerTimezone` 设置，日期时间行为由 ClickHouse 列类型及显式查询转换确定。

## 运行行为

数据库操作通过 `IDataAccess` / `IDataService<T>` 的异步方法执行，查询结果使用 `await foreach` 消费；事务提交、回滚和资源释放均需要等待完成。导入支持内存集合与异步数据源，取消令牌传递给实际操作。 本驱动尚未完整实现，异步 API 不表示所有查询或写入能力均可用；未支持的操作会明确失败，使用前须验证所需功能。

🚨 切勿提交生产连接字符串。请使用部署期配置或秘密提供程序、为数据库身份授予最小权限，并先在可丢弃数据库中验证破坏性变更。

## 兼容性与测试

数据库行为无法完全移植。请在与生产相同的服务器/提供程序版本上验证标识符大小写、空值和比较规则、日期时间精度、生成值、事务语义与批量导入约束。连接设置和命令创建测试可离线运行；查询和批量导入的端到端验证需要显式配置的一次性 ClickHouse 数据库。

## 延伸阅读

- [Zongsoft.Data 使用指南](../../README.zh-Hans.md)
- [驱动实现规则](../../AGENTS.md)
- [数据引擎实现技能](../../SKILL.md)
- [ClickHouse 提供程序文档](https://clickhouse.com/docs/integrations/csharp)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

清单将驱动加入 `/Workbench/Data/Drivers` 及连接设置注册表。配置前文的准确驱动键、部署应用映射，再从宿主获取 IDataAccess。创建或迁移数据库仍是应用的显式操作。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Data.ClickHouse` | [Zongsoft.Data.ClickHouse.plugin](src/Zongsoft.Data.ClickHouse.plugin) |
| 文件复制及依赖 | [Zongsoft.Data.ClickHouse.deploy](src/Zongsoft.Data.ClickHouse.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data clickhouse]
nuget:Zongsoft.Data.ClickHouse
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Data.ClickHouse.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

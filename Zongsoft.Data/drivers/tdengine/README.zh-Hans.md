# Zongsoft.Data.TDengine 数据驱动插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Data.TDengine)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Data.TDengine)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**ata.**TD**engine](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Data/drivers/tdengine) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架中的数据引擎的底层驱动，提供了 [_**TD**engine_](https://www.taosdata.com) 时序库访问的相关功能，对上层应用透明，只需将该插件库部署到应用程序的插件目录中即可。


## 适用场景

当 Zongsoft 数据模型存储于 TDengine 时选择本包。上层应用仍面向 `IDataAccess`/`IDataService<T>`；本驱动通过 WebSocket 访问 TDengine。

本驱动仅支持 WebSocket，默认连接 taosAdapter 的 6041 端口；显式配置 Native 会被拒绝。超级表、标签、时间戳精度和服务器兼容性都属于数据契约。

## 安装与插件部署

```shell
dotnet add package Zongsoft.Data.TDengine
```

请将 `Zongsoft.Data.TDengine.plugin` 与程序集一起部署。清单会在框架工作台中注册连接设置解析器和数据驱动；只有 NuGet 引用而缺少插件清单，不会在插件宿主中激活驱动。

## 连接配置

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

`driver` 不区分大小写，但建议使用规范拼写 `TDengine`。`value` 遵循 [Core 设置契约](../../../Zongsoft.Core/docs/settings.zh-Hans.md)，包含显式驱动参数和扩展属性。连接配置只接受本驱动公开的设置项，WebSocket 连接要求 taosAdapter 可访问。

> 💡 实体映射、条件、分页、事务和 `IDataAccess` 用法统一参阅 [Zongsoft.Data 指南](../../README.zh-Hans.md)。切换驱动不应迫使应用服务依赖提供程序连接类型。

连接字符串使用[本驱动公开的设置模型](src/Configuration/TDengineConnectionSettings.cs)，不保证支持底层 SDK 的全部关键字。示例仅指向本机测试环境；需要先准备数据库、账号及权限，并替换明确标注的测试凭据。不要把示例当作服务器初始化脚本。

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

## 运行行为

数据库操作通过 `IDataAccess` / `IDataService<T>` 的异步方法执行，查询结果使用 `await foreach` 消费；事务提交、回滚和资源释放均需要等待完成。导入支持内存集合与异步数据源，取消令牌传递给实际操作。 本驱动不支持数据库事务；环境事务不会使 TDengine 写入具备回滚能力。

🚨 切勿提交生产连接字符串。请使用部署期配置或秘密提供程序、为数据库身份授予最小权限，并先在可丢弃数据库中验证破坏性变更。

## 兼容性与测试

连接器依赖为社区维护的 [TDengine.Connector.Async 3.2.1.7](https://www.nuget.org/packages/TDengine.Connector.Async/3.2.1.7)，部署时使用与本驱动匹配的依赖版本。

数据库行为无法完全移植。请在与生产相同的服务器/提供程序版本上验证标识符大小写、空值和比较规则、日期时间精度、生成值、事务语义与批量导入约束。仓库测试需要显式配置可丢弃数据库，不属于普通离线单元测试。

## 延伸阅读

- [Zongsoft.Data 使用指南](../../README.zh-Hans.md)
- [驱动实现规则](../../AGENTS.md)
- [数据引擎实现技能](../../SKILL.md)
- [TDengine 提供程序文档](https://docs.tdengine.com/developer-guide/connectors-reference/csharp/)
## 单元测试

> [!IMPORTANT]
> 数据库测试需要可访问的 TDengine 和 taosAdapter，并使用 WebSocket 端口。无需安装本机 taosc 客户端；服务器账号、数据库权限和映射仍须准备就绪。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

清单将驱动加入 `/Workbench/Data/Drivers` 及连接设置注册表。配置前文的准确驱动键、部署应用映射，再从宿主获取 IDataAccess。创建或迁移数据库仍是应用的显式操作。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Data.TDengine` | [Zongsoft.Data.TDengine.plugin](src/Zongsoft.Data.TDengine.plugin) |
| 文件复制及依赖 | [Zongsoft.Data.TDengine.deploy](src/Zongsoft.Data.TDengine.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft data]
nuget:Zongsoft.Data

[plugins zongsoft data tdengine]
nuget:Zongsoft.Data.TDengine
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Data.TDengine.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

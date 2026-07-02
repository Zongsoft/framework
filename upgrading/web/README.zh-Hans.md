# Zongsoft.Upgrading.Web Web 服务端包管理器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**W**eb](https://github.com/Zongsoft/framework/tree/main/upgrading/web) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 自动升级体系的 Web 服务端包管理器。

它负责存储应用和发布元数据，通过配置的 Zongsoft 文件系统存储托管包文件，提供发布和实例的管理服务，并暴露应用端升级器使用的发布发现接口。

## 职责

- 管理应用、版本分发、发布、发布属性、发布执行器和发布状态。
- 导入 `dotnet-upgrade pack` 生成的 `.manifest` 清单文件。
- 为已有发布上传包文件，并回写文件路径、大小和校验码。
- 响应 `Zongsoft.Upgrading.Upgrader` 的发布发现请求。
- 按应用名、版本分发名、平台、架构、可见性、发布状态、弃用状态、版本范围、包路径和包大小筛选发布。
- 在返回发布给特定应用实例前执行可选的评估器逻辑。

## 安装

将 NuGet 包安装到 Zongsoft Web 应用中：

```shell
dotnet add package Zongsoft.Upgrading.Web
```

该包包含插件、选项、部署和数据映射制品：

- `Zongsoft.Upgrading.Web.plugin`
- `Zongsoft.Upgrading.Web.option`
- `Zongsoft.Upgrading.Web.deploy`
- `Zongsoft.Upgrading.mapping`

插件依赖：

- `Zongsoft.Data`
- `Zongsoft.Data.SQLite`

## 配置

默认选项文件声明了发布存储位置和 Upgrading 数据库连接：

```xml
<options>
	<option path="/Upgrading/Settings">
		<setting setting.name="server" value="storage=zfs.s3:/upgrading/releases" />
	</option>

	<option path="/Data">
		<connectionSettings>
			<connectionSetting connectionSetting.name="Upgrading" driver="SQLite"
			                   value="Database=zongsoft.upgrading.db;PRAGMA:optimize;PRAGMA:journal_mode=WAL;PRAGMA:synchronous=normal;PRAGMA:temp_store=memory;" />
		</connectionSettings>
	</option>
</options>
```

设置 | 说明
----|-----
`/Upgrading/Settings/server:storage` | 上传发布包文件时使用的存储根路径。
`/Data/connectionSettings/Upgrading` | 模块和数据服务使用的数据访问连接。

## 发现接口

应用端升级器会调用 `Upgrading` 区域下的 `Upgrader` 控制器：

```http
GET /Upgrading/Upgrader/{name}/{edition?}?Platform=Windows&Architecture=X64&CurrentlyVersion=1.0.0
```

参数 | 来源 | 说明
----|------|-----
`name` | 路由 | 应用名称。
`edition` | 路由 | 可选版本分发名。空值或 `_` 匹配无版本分发名的发布。
`Platform` | 查询 | 目标操作系统平台。
`Architecture` | 查询 | 目标 CPU 架构。
`CurrentlyVersion` | 查询 | 当前应用版本。返回的发布版本必须高于该版本。
`UpgradingVersion` | 查询 | 可选的最高升级目标版本。
硬件字段 | 查询 | 升级器提交的可选指纹值，如主板、处理器、网络、内存或存储标识。

响应是 XML 发布流，应用端升级器可通过共享的 `Release` 读取器解析。

## 评估器

评估器允许服务端判断某个发布是否适用于当前请求。发布可指定评估器名称和评估器设置。当 `Module.Current.Evaluators` 中注册了同名评估器时，Web 模块会传入：

- 应用名称；
- 评估器设置；
- 请求查询参数；
- 请求头。

只有评估器返回 `true` 的发布才会返回给升级器。

已注册评估器可通过以下接口查看：

```http
GET /Upgrading/Upgrader/Evaluators
```

## 发布管理

`Releases` 控制器扩展了 Zongsoft 服务控制器约定，并增加了两个升级相关操作：

操作 | 说明
----|-----
导入清单 | 上传 `.manifest` 文件并指定 `format=manifest`，创建发布元数据、属性和执行器。
上传包文件 | 为已有发布上传包内容。服务会按配置的存储根路径保存包文件，计算校验码，更新大小并记录最终包 URL。

包文件存储路径按发布身份生成：

```text
{storage}/{小写发布名}/{发布名}[-版本分发名]@{版本}_{runtime}
```

例如：

```text
zfs.s3:/upgrading/releases/zongsoft.daemon/Zongsoft.Daemon-stable@1.1.0_win-x64
```

## 发布筛选

只有满足以下条件的发布才会返回给升级器：

- 可见；
- 已发布；
- 未弃用；
- 应用名匹配；
- 平台和架构匹配；
- 版本分发名匹配，或请求版本分发名为空或 `_` 时匹配无版本分发名发布；
- 指定 `CurrentlyVersion` 时，发布版本高于当前版本；
- 指定 `UpgradingVersion` 时，发布版本不高于目标版本；
- 已关联存在的包文件路径；
- 已关联大于零的包大小。

## 典型流程

1. 使用 [Zongsoft.Tools.Upgrader](../tool/README.zh-Hans.md) 制作应用发布包。
2. 将生成的 `.manifest` 清单文件导入 Web 服务端包管理器。
3. 为导入的发布上传对应包文件。
4. 通过管理 API 或数据服务将发布标记为可见且已发布。
5. 将应用端配置为使用 `Web` 通道，并指向 `/Upgrading/Upgrader` 接口。

## 注意事项

- `storage` 设置应指向 Web 应用可访问的文件系统提供器。
- 包文件上传会根据已存储文件更新校验码，因此手动修改包文件后应重新上传或重新计算。
- 评估器会同时收到查询参数和请求头，适合实现灰度发布、实例定向或硬件绑定授权策略。

## 许可

本项目采用 [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可协议。

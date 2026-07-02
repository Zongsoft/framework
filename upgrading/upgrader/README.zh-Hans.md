# Zongsoft.Upgrading.Upgrader 应用端升级器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Upgrader)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Upgrader)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**U**pgrader](https://github.com/Zongsoft/framework/tree/main/upgrading/upgrader) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的应用端自动升级器。

它被加载到 Zongsoft 宿主应用后，会按周期检查配置的发布通道，下载匹配的升级包，解压文件，创建部署描述文件，启动部署器程序，然后关闭当前应用程序，让后续部署工作在独立进程中完成。

## 职责

- 解析当前应用身份：应用名、版本分发名、版本号、平台、架构、应用目录和应用类型。
- 从配置的 `File` 或 `Web` 通道发现候选发布。
- 选择高于当前版本且不高于可选目标版本的全量发布和增量发布。
- 将发布包下载到应用专属临时目录。
- 在部署前校验包大小和校验码。
- 将升级包解压到临时 `.app` 目录。
- 在应用目录写入 `.deployment` 部署描述文件。
- 启动 `Zongsoft.Upgrading.Deployer` 并停止当前宿主进程。

## 安装

将 NuGet 包安装到需要自升级的应用程序中：

```shell
dotnet add package Zongsoft.Upgrading.Upgrader
```

该包同时包含 Zongsoft 插件宿主使用的插件和选项制品：

- `Zongsoft.Upgrading.Upgrader.plugin`
- `Zongsoft.Upgrading.Upgrader.option`
- `Zongsoft.Upgrading.Upgrader.deploy`

## 配置

默认选项文件在 `/Upgrading` 下声明了两个连接设置：

```xml
<options>
	<option path="/Upgrading">
		<connectionSettings default="File">
			<connectionSetting connectionSetting.name="Web"
			                   value="url=http://127.0.0.1:8069/Upgrading/Upgrader;timeout=30s" />

			<connectionSetting connectionSetting.name="File"
			                   value="url=zfs.s3:/upgrading/releases/" />
		</connectionSettings>
	</option>
</options>
```

设置 | 说明
----|-----
`default` | 调用 `Upgrader.UpgradeAsync()` 且未显式指定通道时使用的通道。
`Web:url` | Web 服务端包管理器的发现接口基地址。
`Web:timeout` | HTTP 超时时间，`30s` 这类值由 Zongsoft 时间跨度工具解析。
`File:url` | 包含发布清单和包文件的文件系统根 URL。

## 插件启动

包内插件会在 `/Workbench/Startup` 下注册一个 `Upgrader` 工作器：

```xml
<extension path="/Workbench/Startup">
	<object name="Upgrader" period="10m" type="Zongsoft.Upgrading.Upgrader, Zongsoft.Upgrading.Upgrader" />
</extension>
```

该工作器按 `period` 周期检查升级。如果周期大于或等于五分钟，还会在启动约十秒后安排一次短延迟检查。

## 手动调用

应用程序也可以直接调用升级器：

```csharp
using Zongsoft.Upgrading;

if(await Upgrader.UpgradeAsync("Web", cancellationToken))
	Upgrader.Deploy();
```

当升级已准备完成，或者已有部署描述文件正在等待部署时，`UpgradeAsync` 返回 `true`。

## 通道行为

### File 通道

`File` 通道会按以下顺序扫描配置的根 URL：

1. `{root}/{应用名}/*.manifest`
2. `{root}/{应用类型}/{应用名}/*.manifest`
3. `{root}/{应用类型}/{应用名}*.manifest`
4. `{root}/{应用名}*.manifest`

对于每个清单文件，它会按清单所在位置解析包文件路径，并将解析出的下载 URL 写入发布属性。

### Web 通道

`Web` 通道会调用：

```text
{base-url}/{应用名}/{版本分发名}?Name=...&Edition=...&Platform=...&Architecture=...&CurrentlyVersion=...
```

请求还会携带硬件指纹信息，服务端评估器可以据此判断某个发布是否适合特定应用实例。

## 发布选择

升级器只接受满足以下条件的发布：

- 应用名匹配；
- 平台匹配；
- 架构匹配；
- 指定版本分发名时，版本分发名匹配；
- 版本号高于当前应用版本；
- 指定目标版本时，版本号不高于目标版本。

如果存在全量发布，则选择最新的全量发布作为主干，并只应用版本高于该主干的增量发布。如果没有选中全量发布，则按版本升序应用匹配的增量发布。

## 部署交接

部署器程序必须位于：

```text
{应用目录}/.deployer/Zongsoft.Upgrading.Deployer
```

在 Windows 上可执行文件为 `Zongsoft.Upgrading.Deployer.exe`。在 Linux 上，升级器通过 `systemd-run` 启动部署器，以便部署器在应用进程退出后继续运行。

升级器会向部署器传递这些关键参数：

参数 | 说明
----|-----
`site` | 来自应用配置的站点标识。
`app.id` | 当前进程编号。
`app.name` | 当前应用名称。
`app.type` | 应用类型，如 `Web`、`Daemon` 或 `Terminal`。
`app.path` | 应用根目录。
`host.path` | 当前宿主可执行程序路径。
`host.args#n` | 原始宿主命令行参数。
`daemon` | 部署器重启逻辑使用的可选服务名。
`deployment` | `.deployment` 描述文件的完整路径。

## 执行器

升级器会注册内建执行器命令：

- `Copy`
- `Move`
- `Link`
- `Delete`

执行器在发布清单中声明，之后由部署器在 `Deploying` 和 `Deployed` 阶段调用。

## 注意事项

- 升级包应包含与目标平台兼容的部署器可执行程序。
- 全量发布会在部署时清空应用目录；持久化数据、日志和配置应放在不会被升级包替换的位置，或从包中排除。
- 运行中的宿主应用需要能在升级器关闭超时时间内正常停止。

## 许可

本项目采用 [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可协议。

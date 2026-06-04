# Zongsoft Upgrading 自动升级库

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading](https://github.com/Zongsoft/framework/tree/main/upgrading) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的应用程序自动升级库。

它包含应用端升级流程、独立运行的部署器程序、服务端升级包管理能力，以及用于制作、校验、发布升级包的命令行工具。

## 组成

组件 | 包或项目 | 职责
----|---------|-----
应用端升级器 | [Zongsoft.Upgrading.Upgrader](upgrader/README-zh_CN.md) | 发现新版本、下载升级包、校验校验码、解压文件、写入部署描述文件、启动部署器并退出当前应用程序。
应用端部署器 | [Zongsoft.Upgrading.Deployer](deployer/README-zh_CN.md) | 一个 Native AOT 可执行程序，负责等待宿主应用退出、将解压后的文件复制到应用目录、执行部署器命令、重启应用并结束自身。
Web 服务端包管理器 | [Zongsoft.Upgrading.Web](web/README-zh_CN.md) | 托管发布元数据和升级包文件，管理应用、发布、实例，并提供升级器使用的发现和下载接口。
升级包工具 | [Zongsoft.Tools.Upgrader](tool/README-zh_CN.md) | 制作升级 ZIP 包和 `.manifest` 清单文件，重新计算校验码，并将升级包发布到支持的通道。

## 升级流程

1. 构建或发布目标应用程序。
2. 使用 `dotnet-upgrade pack` 生成升级包和 `.manifest` 清单文件。
3. 将升级包发布到文件存储位置或 Web 服务端包管理器。
4. 运行中的应用程序加载 `Zongsoft.Upgrading.Upgrader`，并按配置周期检查 `File` 或 `Web` 通道。
5. 升级器按照应用名、版本分发名、平台、架构和版本范围筛选可用发布。
6. 选中的升级包会被下载、校验并解压到临时 `.app` 目录。
7. 升级器在应用目录中写入 `.deployment` 部署描述文件。
8. 升级器启动 `Zongsoft.Upgrading.Deployer` 并关闭当前应用程序。
9. 部署器等待宿主进程退出，根据发布类型决定是否清空应用目录，然后复制解压文件、执行 `Deploying` 和 `Deployed` 执行器，并重启应用程序。

## 升级包

每个发布包含：

- 一个包含应用程序文件的 ZIP 包；
- 一个由升级包工具生成的 `.manifest` 元数据清单；
- 可选的标签、标题、概述、说明、扩展属性和部署执行器。

发布身份由以下信息组成：

- 应用名称；
- 版本分发名，如 `stable`、`community`、`standard`、`professional`、`enterprise`；
- 版本号；
- 平台，如 `windows`、`linux`；
- 架构，如 `x64`、`x32`、`arm64`、`arm32`；
- 发布类型，即 `Fully` 或 `Delta`。

### 清单文件示例

```xml
<?xml version="1.0" encoding="UTF-8"?>

<release
	name="Zongsoft.Hosting.Terminal"
	kind="Fully"
	edition=""
	version="1.1.0"
	size="123456"
	path="Zongsoft.Hosting.Terminal@1.1.0_win-x64.zip"
	checksum="SHA1:1234567890ABCDEF"
	platform="Windows"
	architecture="X64"
	deprecated="false"
	creation="2026-04-10T10:30:59">

	<title></title>
	<summary></summary>
	<description></description>

	<tags>
		<tag></tag>
	</tags>

	<properties>
		<property name="" type="">
		<![CDATA[
		]]>
		</property>
	</properties>

	<executors>
		<executor event="">
		<![CDATA[
		]]>
		</executor>
	</executors>
</release>
```

## 发布类型

- `Fully`：全量发布。部署时会先清空应用目录，再复制解压后的升级包文件。
- `Delta`：增量发布。部署时不会先清空应用目录，而是将解压后的文件覆盖复制到应用目录。

## 通道

应用端升级器当前支持两个发现与下载通道：

- `File`：从配置的文件系统 URL 读取清单和包文件，支持 `zfs.s3:/...` 等 Zongsoft 文件系统提供器。
- `Web`：调用 Web 服务端包管理器接口，并根据服务端返回的 URL 下载包文件。

升级包工具当前支持发布到：

- `amazon.s3`。
- `web`。

## 执行器命令

发布清单可以包含在部署事件中运行的执行器命令。升级器和部署器都会注册内建命令：

- `Copy`
- `Move`
- `Link`
- `Delete`

部署器会在 `Deploying` 和 `Deployed` 事件中调用执行器，可用于更新服务文件、维护符号链接、清理文件或执行其他受控部署任务。

## 文档

- [升级器](upgrader/README-zh_CN.md)
- [部署器](deployer/README-zh_CN.md)
- [Web端](web/README-zh_CN.md)
- [工具](tool/README-zh_CN.md)

## 许可

本项目采用 [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可协议。

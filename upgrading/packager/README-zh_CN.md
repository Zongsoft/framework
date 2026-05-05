# 自动升级打包器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Packager)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Packager)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**P**ackager](https://github.com/Zongsoft/framework/tree/main/upgrading/packager/Zongsoft.Upgrading.Packager) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级插件库的打包器。

## 打包

### 命令选项

- `--name` _必须项_，应用名称。
	> 💡 注意：该名称必须与发布的应用名一致，因为升级器以该名称作为应用匹配的依据。
- `--kind` _必须项_，发布种类：
	- `Fully` 全量发布
	- `Delta` 增量发布
- `--edition` _可选项_，版本名称，表示应用的分发名，默认空。
	> 常见的有：`stable`，`community`, `standard`, `professional`, `enterprise` 等。
- `--version` _必须项_，版本号码，由最多四段整数组成，譬如：`1.2.3`、`1.2.3.4`
- `--checksum` _可选项_，校验码算法名，限定为 `sha1`, `sha256`, `sha384`, `sha512`，默认为 `sha1`。
- `--platform` _必须项_，平台标识，表示操作系统标识，限定为 `windows`, `linux`
- `--framework` _必须项_，框架标识，表示 _.NET_ 框架标识，通常为 `net10.0`, `net9.0`, `net8.0`
- `--architecture` _可选项_，架构体系，表示 _CPU_ 架构标识，限定为 `x64`, `x32`, `arm64`, `arm32`，默认为 `x64`。
- `--source` _必须项_，发布包源目录。
- `--output` _可选项_，发布包文件路径，通常不需要指定，由工具根据相关选项自动生成：
	> - 未指定 `edition` 选项：`{name}@{version}_{platform}-{architecture}`，譬如：`zongsoft.daemon@1.2.3_win-x64`
	> - 指定了 `edition` 选项：`{name}({edition})@{version}_{platform}-{architecture}`，譬如：`zongsoft.daemon(stable)@1.2.3_linux-x64`
- `--tags` _可选项_，标签集合，以 _逗号_ 或 _分号_ 分隔。
- `--title` _可选项_，发布标题。
- `--summary` _可选项_，发布概述，可以是文本文件路径或概述内容。
- `--description` _可选项_，发布说明，可以是文本文件路径或说明内容。
- `--overwrite` _可选项_，是否覆盖，如果启用则表示当目标文件存在则覆盖它，默认不覆盖。

### 命令参数

如果未指定任何命令参数，则将 `--source` 选项所指向源目录中的所有文件及下级目录的所有内容全部打包到输出文件。

- 如果指定了命令参数，则只打包参数所指定的目录或文件。
- 命令参数通常为相对于源目录的路径，但也可以是绝对路径。
- 命令参数路径支持文件部分的通用通配符 _(即 `*` 和 `?` 符)_。
- 命令参数支持重命名打包后的目录和文件名，使用 `:` 冒号分隔源路径与重命名的名称，其规则如下：
	- 重命名的名称为 _空_ 或 `~` 或 `/` 表示包的根目录；
	- 对于含通配符的路径则重命名部分表示为包中目录。

### 范例

- 后台程序 _(全量)_

```shell
dotnet-pack
	--name:Zongsoft.Daemon
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:fully
	--source:"D:\\Zongsoft\\hosting\\daemon\\bin\\$(compilation)\\$(framework)"
	--output:../../../
	--tags:tag1,tag2,tagX
	--executor.link@deployed:"$(name).service /Zongsoft/hosting/.deploy/$(scheme)/systemd/$(name).service"
```

- 后台程序 _(增量)_

```shell
dotnet-pack
	--name:Zongsoft.Daemon
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:delta
	--source:"D:/Zongsoft/hosting/daemon/bin/$(compilation)/$(framework)"
	--output:../../../
	plugins/zongsoft/upgrader
	plugins/zongsoft/externals/redis
	plugins/zongsoft/externals/hangfire
```

- Web 程序 _(全量)_

```shell
dotnet-pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:fully
	--source:./
	--output:./$(name)($(edition))@$(version)_$(runtime).zip
	--tags:tag1,tag2,tagX
	--executor.link@deployed:"zongsoft.web.service /Zongsoft/hosting/.deploy/default/systemd/zongsoft.web.service"
	mime
	appsettings.json
	web*.config
	web*.option
	wwwroot
	plugins
	bin/$(compilation)/$(framework):~
```

- Web 程序 _(增量)_

```shell
dotnet-pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--compilation:Debug
	--framework:net10.0
	--platform:windows
	--architecture:x64
	--kind:delta
	--source:.
	--output:.
	web.option
	bin/$(compilation)/$(framework)/*:~
	plugins/zongsoft/upgrader
	plugins/zongsoft/externals/redis
	plugins/zongsoft/externals/hangfire
```

## 校验

> 💡 如果手动修改过打包文件的内容，则需要重新计算校验码。

### 范例

```shell
dotnet-pack checksum --algorithm:sha1 Zongsoft.Daemon(stable)@1.1.0_win-x64.zip
```

## 发布

### 范例

- _**A**mazon.**S3**_ 文件系统

```shell
dotnet-pack publish
	--channel:zfs.s3
	--server:127.0.0.1
	--region:cn-north-1
	--access:rustfsadmin
	--secret:rustfsadmin
	--destination:/upgrading/releases/daemon
	Zongsoft.Daemon(stable)@1.1.0_win-x64
```

- _**W**eb_ 发布站点

```shell
dotnet-pack publish
	--channel:web
	--url:localhost:8069/upgrading
	Zongsoft.Daemon(stable)@1.1.0_win-x64
```

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

## 用法

- 终端程序 _(全量)_

```shell
dotnet pack
	--name:Zongsoft.Hosting.Terminal
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--framework:net10
	--platform:windows
	--architecture:x64
	--kind:fully
	--source:./
	--output:./Zongsoft.Hosting.Terminal(stable)@1.1.0_win-x64
```

- 终端程序 _(增量)_

```shell
dotnet pack
	--name:Zongsoft.Hosting.Terminal
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--framework:net10
	--platform:windows
	--architecture:x64
	--kind:delta
	--source:./
	--output:./Zongsoft.Hosting.Terminal(stable)@1.1.0_win-x64
	plugins/upgrading
	plugins/externals/redis
	plugins/externals/scriban
```

- Web 程序 _(全量)_

```shell
dotnet pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--framework:net10
	--platform:windows
	--architecture:x64
	--kind:fully
	--source:./
	--output:./Zongsoft.Hosting.Web(stable)@1.1.0_win-x64
	mime
	appsettings.json
	web.config
	web.option
	wwwroot
	plugins
	bin/$(edition)/${framework}:~
```

- Web 程序 _(增量)_

```shell
dotnet pack
	--name:Zongsoft.Hosting.Web
	--version:1.1.0
	--edition:stable
	--checksum:sha1
	--framework:net10
	--platform:windows
	--architecture:x64
	--kind:delta
	--source:./
	--output:./Zongsoft.Hosting.Web(stable)@1.1.0_win-x64
	web.option
	bin/$(edition)/${framework}/Zongsoft.Web.*:~
	bin/$(edition)/${framework}/Zongsoft.Plugins.*:~
	plugins/upgrading
	plugins/externals/redis
	plugins/externals/scriban
```

- 重新校验

> 💡 如果手动修改过打包文件的内容，则需要重新计算校验码。

```shell
dotnet pack checksum Zongsoft.Hosting.Terminal(stable)@1.1.0_win-x64.zip
```

- 打包发布

```shell
dotnet pack publish
	--channel:file
	--destination:zfs.s3:/upgrading/releases/terminal
	Zongsoft.Hosting.Terminal(stable)@1.1.0_win-x64
```

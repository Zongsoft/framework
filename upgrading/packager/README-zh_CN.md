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

### 打包

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
	web.config
	web.option
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

### 重新校验

> 💡 如果手动修改过打包文件的内容，则需要重新计算校验码。

```shell
dotnet-pack checksum --algorithm:sha1 Zongsoft.Daemon(stable)@1.1.0_win-x64.zip
```

### 打包发布

- _**A**mazone.**S3**_ 文件系统

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

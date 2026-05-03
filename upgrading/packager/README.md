# Zongsoft Upgrading Packager

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Packager)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Packager)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## Overview

[**Z**ongsoft.**U**pgrading.**P**ackager](https://github.com/Zongsoft/framework/tree/main/upgrading/packager/Zongsoft.Upgrading.Packager) is a packager for the automatic upgrade plugin library of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) open-source framework.

## Usage

### Packing

- Daemon program _(fully)_

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

- Daemon program _(delta)_

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

- Web program _(fully)_

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

- Web program _(delta)_

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

### Rechecksum

> 💡 If you have manually modified the contents of the packaged file, you will need to recalculate the checksum.

```shell
dotnet-pack checksum --algorithm:sha1 Zongsoft.Daemon(stable)@1.1.0_win-x64.zip
```

### Publish

- _**A**mazone.**S3**_ File System

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

- _**W**eb_ Site

```shell
dotnet-pack publish
	--channel:web
	--url:localhost:8069/upgrading
	Zongsoft.Daemon(stable)@1.1.0_win-x64
```

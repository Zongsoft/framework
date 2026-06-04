# Zongsoft.Upgrading.Deployer 升级部署器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Deployer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Deployer)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**D**eployer](https://github.com/Zongsoft/framework/tree/main/upgrading/deployer) 是自动升级体系的应用端部署器程序。它以 _**N**ative **AOT**_ 方式发布为独立可执行程序，因此可以在宿主应用退出后继续运行。

部署器接收升级器创建的部署描述文件，等待宿主进程停止，将解压好的升级包文件复制到应用目录，执行部署器命令，重启应用程序，然后结束自身。

## 职责

- 解析升级器传入的命令行参数。
- 等待宿主进程退出后再修改应用文件。
- 加载 `.deployment` 部署描述文件，并在部署期间排他锁定它。
- 加载部署描述文件引用的发布清单。
- 对全量发布清空应用目录，同时保留部署器目录和部署描述文件。
- 将解压后的包文件复制到应用目录。
- 执行发布清单中的 `Deploying` 和 `Deployed` 执行器命令。
- 按应用类型重启宿主应用。

## 部署描述文件

升级器会在应用目录写入一个 `.deployment` 文件。部署器通过 `deployment` 参数接收该文件的完整路径。

```text
Manifest=C:\Users\...\Temp\Zongsoft.Hosting.Terminal\Zongsoft.Hosting.Terminal@1.1.0.manifest
Packages=C:\Users\...\Temp\Zongsoft.Hosting.Terminal\.app
```

字段 | 说明
----|-----
`Manifest` | 升级器保存的发布清单完整路径。
`Packages` | 已解压发布包文件所在目录。

部署器打开该文件时会使用排他锁，并在关闭时删除该描述文件。

## 命令行参数

参数 | 必填 | 说明
----|:----:|-----
`deployment` | ✓ | `.deployment` 部署描述文件完整路径。
`app.id` | - | 宿主进程编号。提供该参数时，部署器会等待该进程退出。
`app.name` | - | 应用名称。
`app.type` | - | 重启逻辑使用的应用类型，如 `Web`、`Daemon` 或 `Terminal`。
`app.path` | - | 应用根目录。
`host.path` | - | 宿主可执行程序路径。
`host.args#n` | - | 原始宿主命令行参数。
`site` | - | 站点标识。
`daemon` | - | 后台服务或 Web 重启逻辑使用的服务名。

示例：

```shell
Zongsoft.Upgrading.Deployer \
	app.id=12345 \
	app.name=Zongsoft.Hosting.Terminal \
	app.type=Terminal \
	app.path=/opt/zongsoft/terminal \
	host.path=/opt/zongsoft/terminal/Zongsoft.Hosting.Terminal \
	deployment=/opt/zongsoft/terminal/.deployment
```

## 部署行为

发布类型 | 行为
--------|-----
`Fully` | 先清空应用目录，但保留部署器目录和 `.deployment` 描述文件，然后复制解压后的文件。
`Delta` | 不先清空应用目录，直接将解压后的文件覆盖复制到现有应用目录。

部署器目录名为 `.deployer`，预期的可执行文件名为：

- Windows：`Zongsoft.Upgrading.Deployer.exe`；
- Linux 及其他类 Unix 平台：`Zongsoft.Upgrading.Deployer`。

## 重启策略

应用类型 | Windows | Linux 或 FreeBSD
--------|---------|-----------------
`Web` | 通过 `appcmd recycle apppool {app.name}` 回收 IIS 应用程序池。 | 通过 `systemctl start {daemon}` 启动服务。
`Daemon` | 通过 `sc start {daemon}` 启动服务。 | 通过 `systemctl start {daemon}` 启动服务。
`Terminal` | 使用原始参数启动原宿主可执行程序。 | 使用原始参数启动原宿主可执行程序，并等待其退出，以便控制台输入可用。
其他 | 使用原始参数启动原宿主可执行程序。 | 使用原始参数启动原宿主可执行程序。

如果没有提供 `daemon` 参数，部署器会尝试在应用目录中查找单个 `*.service` 文件，然后查找文件名以 `app.name` 开头的服务文件；如果都找不到，则回退为 `app.name`。

## 执行器

部署器会在部署前注册以下内建命令：

- `Copy`
- `Move`
- `Link`
- `Delete`

发布清单中的执行器会在以下阶段调用：

- `Deploying`：清理完成后、复制包文件前；
- `Deployed`：包文件复制完成后、重启宿主应用前。

## 发布

### Windows 平台

#### 使用 Visual Studio 发布

1. 使用 _**M**icrosoft **V**isual **S**tudio 2026_ 打开 [Zongsoft.Upgrading.Deployer.slnx](./Zongsoft.Upgrading.Deployer.slnx) 解决方案。
2. 在 _“解决方案资源管理器”_ 中选择该项目，右击并选择 _“发布”_。
3. 在 _“发布”_ 窗体中点击 _“发布”_ 按钮。

#### 使用命令行发布

在 `deployer` 目录中运行下列命令：

```cmd
dotnet publish Zongsoft.Upgrading.Deployer.csproj ^
	--self-contained ^
	--runtime win-x64 ^
	--framework net10.0 ^
	--configuration Release ^
	-p:PublishAot=true
```

发布后的可执行文件位于：

```text
bin\Release\net10.0\win-x64\publish\Zongsoft.Upgrading.Deployer.exe
```

### Linux 平台

#### 容器内发布

1. 运行 [framework.start](./../../framework-start.cmd) 脚本，启动一个包含 _.NET 10 SDK_ 的 _**L**inux_ Alpine 容器。

	先确保该容器已经加载完成：

	```cmd
	podman ps -a --pod
	podman logs zongsoft-framework
	```

2. 进入名为 `zongsoft-framework` 的容器：

	```cmd
	podman exec --workdir /Zongsoft/framework/upgrading/deployer -it zongsoft-framework sh
	```

3. 发布 _**L**inux_ `x64` 版本部署器：

	```shell
	./publish.linux-x64.sh
	```

4. 运行 `exit` 退出容器。
5. 运行 [framework.stop](./../../framework-stop.cmd) 脚本关闭该容器。

#### 容器外发布

1. 运行 [framework.start](./../../framework-start.cmd) 脚本，启动一个包含 _.NET 10 SDK_ 的 _**L**inux_ Alpine 容器。

	先确保该容器已经加载完成：

	```cmd
	podman ps -a --pod
	podman logs zongsoft-framework
	```

2. 运行下列发布脚本之一：

	- 在 _**P**ower**S**hell_ 中运行 [publish.linux-x64.ps1](./publish.linux-x64.ps1)；
	- 在 _CMD_ 中运行 [publish.linux-x64.cmd](./publish.linux-x64.cmd)。

3. 运行 [framework.stop](./../../framework-stop.cmd) 脚本关闭该容器。

## 注意事项

- 部署器不应被全量发布覆盖。请将部署器放在 `.deployer` 目录中，以便清理和复制逻辑保留它。
- 部署器等待宿主进程退出的时间约为 60 秒。
- 服务重启命令通常需要提升权限。

## 许可

本项目采用 [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) 许可协议。

# Zongsoft.Upgrading.Deployer 升级部署器

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Deployer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Deployer)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**U**pgrading.**D**eployer](https://github.com/Zongsoft/framework/tree/main/upgrading/upgrader/Zongsoft.Upgrading.Deployer) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的自动升级部署器，是一个以 _**N**ative **AOT**_ 发布的独立程序。

## 发布

### Windows 平台

1. 使用 _**M**icrosoft **V**isual **S**tuido 2026_ 打开 [_Zongsoft.Upgrading.Deployer.slnx_](./Zongsoft.Upgrading.Deployer.slnx) 解决方案；
2. 在 “_解决方案资源管理器_” 中选择该项目，然后鼠标右击，在弹出的菜单中选择 “_发布_” 菜单项；
3. 在 “_发布_” 窗体中点击 “_发布_” 按钮。

### Linux 平台

#### 容器内发布

1. 运行 [_framework.start_](./../../framework-start.cmd) 脚本启动一个包含 _.NET 10 SDK_ 的 _alpine_ 的 _**L**inux_ 容器；
	> 先确保该容器已经加载完成：
	> - `podman ps -a --pod`
	> - `podman logs zongsoft-framework`

2. 运行下面命令进入名为 `zongsoft-framework` 容器的虚拟机：
	```cmd
	podman exec --workdir /Zongsoft/framework/upgrading/deployer -it zongsoft-framework sh
	```
3. 在虚拟机中执行下面命令，发布 _**L**inux_ 的 `X64` 版本的部署器程序。
	```shell
	./publish.linux-x64.sh
	```
4. 发布完成后，通过 `exit` 命令退出虚拟机；
5. 运行 [_framework.stop_](./../../framework-stop.cmd) 脚本关闭该容器。


#### 容器外发布

1. 运行 [_framework.start_](./../../framework-start.cmd) 脚本启动一个包含 _.NET 10 SDK_ 的 _alpine_ 的 _**L**inux_ 容器；
	> 先确保该容器已经加载完成：
	> - `podman ps -a --pod`
	> - `podman logs zongsoft-framework`

2. 运行发布脚本 _(下列脚本二选一)_
	> - 在 _**P**ower**S**hell_ 中运行 [publish.linux-x64.ps1](./publish.linux-x64.ps1) 脚本；
	> - 在 _CMD_ 中运行 [publish.linux-x64.cmd](./publish.linux-x64.cmd) 脚本。

3. 运行 [_framework.stop_](./../../framework-stop.cmd) 脚本关闭该容器。

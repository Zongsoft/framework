# Zongsoft.Externals.Garnet 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Garnet)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Garnet)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Garnet` 以 Zongsoft `WorkerBase` 托管 [Microsoft Garnet](https://microsoft.github.io/garnet/)。Garnet 是高性能、兼容 Redis 协议的数据存储；本适配器负责服务器启停，并把框架设置转换为 Garnet 命令行选项。

## 安装与配置

```shell
dotnet add package Zongsoft.Externals.Garnet
```

请部署插件和选项产物。在 `/Externals/Garnet` 下配置具名服务器：

```xml
<option path="/Externals/Garnet">
	<server server.name="cache"
	        value="bind=127.0.0.1;port=6379;auth=Password;password=REPLACE_ME;lua=true" />
</option>
```

通过应用 Worker 生命周期启动 `GarnetServer("cache")`。它的 `Setting` 属性可独立于 Worker 名称选择设置。相对路径选项从适配器程序集目录解析，`~/` 从应用根目录解析。

## 生命周期与持久化

启动会创建底层 `Garnet.GarnetServer` 并调用 `Start`，停止则释放它。适配器把 `Address`、`EnableAOF`、`CheckpointDir`、`EnableTLS` 等友好名称映射为 Garnet 选项，同时透传原生选项名。

> 💡 兼容 Redis 协议不表示全部 Redis 命令、模块、持久化模式或客户端行为完全相同。请验证应用实际使用的命令集。

🚨 随包密码只是占位值。请绑定可信网卡、明确设置身份验证与 TLS、保护 Checkpoint/AOF 目录、限制模块加载，并在保存持久数据前测试恢复。进程内服务器与宿主共享故障和资源边界。

## 验证

使用可丢弃目录与端口，启动 Worker，以兼容客户端连接并执行所需命令，再停止；启用持久化时还要重启验证恢复。投产前测试内存/连接限制、关闭时间和损坏 Checkpoint 的处理。

- 配置文件说明
	> [Garnet 默认值](https://github.com/microsoft/garnet/blob/main/libs/host/defaults.conf)

- 配置参数映射
	> [Garnet 选项模型](https://github.com/microsoft/garnet/blob/main/libs/host/Configuration/Options.cs)

- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

本插件挂载启动工作器，启动宿主前应配置 `/Externals/Garnet`；加载工作器可能启动内嵌服务器并打开监听端口。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Garnet` | [Zongsoft.Externals.Garnet.plugin](src/Zongsoft.Externals.Garnet.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Garnet.deploy](src/Zongsoft.Externals.Garnet.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals garnet]
nuget:Zongsoft.Externals.Garnet
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Garnet.plugin`、`Zongsoft.Externals.Garnet.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

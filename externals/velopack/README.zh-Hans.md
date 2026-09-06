# Zongsoft.Externals.Velopack 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Velopack)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Velopack)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Velopack` 把 [Velopack](https://velopack.io/) 桌面应用升级器接入 Zongsoft 应用与 Worker 生命周期。它会尽早初始化 Velopack、从框架连接设置解析更新源、定期检查并下载版本，随后应用更新并重启已安装应用。

## 安装与配置

```shell
dotnet add package Zongsoft.Externals.Velopack
```

```xml
<option path="/Externals/Velopack">
	<connectionSettings default="current">
		<connectionSetting connectionSetting.name="current"
		                   driver="velopack"
		                   value="source=web;url=https://updates.example.invalid/releases;period=300s" />
	</connectionSettings>
</option>
```

`source` 选择 `IVelopackSourceFactory`，`url` 等设置配置该来源，`period` 控制检查周期。初始化器在 Windows 与 Linux 上配置 `ApplicationLocator`；`Upgrader` 只在 Velopack 判定为有效安装应用时运行，并阻止重叠检查。

## 更新生命周期

Worker 启动 Timer；当常规周期至少五分钟时，还会在 30 秒后提前检查一次。随后调用 `CheckForUpdatesAsync`、下载所选版本，并执行 `ApplyUpdatesAndRestart`。失败会记录日志，后续周期可再次尝试。

> 💡 未从 Velopack 安装目录运行的开发构建会被有意忽略。应测试完整安装包、Channel 与 Feed，不能期待 `dotnet run` 自升级。

🚨 应用更新会重启进程并替换安装文件。请签名并发布可信包、要求 HTTPS、保护 Feed 凭据、验证回滚/恢复、协调未完成工作，开发测试绝不能指向生产 Channel。

## 兼容性

客户端、打包工具与 Feed Schema 必须使用兼容 Velopack 版本。应在每个目标系统测试全新安装、从所有受支持旧版升级、无更新、中断下载、损坏包、磁盘/权限不足与重启行为。

## 参考资料

《Velopack 文档》
> https://docs.velopack.io

《Velopack 参考》
> https://docs.velopack.io/reference

《Velopack 源码》
> https://github.com/velopack/velopack

配套 [Web Feed 包](src/web/README.zh-Hans.md)可从宿主目录发布版本元数据。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

本插件贡献启动升级器，只应为已由 Velopack 安装的应用配置源及周期。启用后可能下载更新并重启进程，它独立于 framework/upgrading。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Velopack` | [Zongsoft.Externals.Velopack.plugin](src/Zongsoft.Externals.Velopack.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Velopack.deploy](src/Zongsoft.Externals.Velopack.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals velopack]
nuget:Zongsoft.Externals.Velopack
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Velopack.option`、`Zongsoft.Externals.Velopack.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

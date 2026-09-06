# Zongsoft.Externals.Hangfire.Storages.Redis 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Storages.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Storages.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Hangfire.Storages.Redis` 为 [Zongsoft Hangfire 集成](../..)提供基于 Redis 的持久化存储。它将 `Hangfire.Redis.StackExchange` 适配到 Zongsoft 连接设置，并把生成的 `RedisStorage` 注册为 Hangfire 的 `JobStorage`。

## 配置

请同时加载 `Zongsoft.Externals.Hangfire.Storages.Redis.plugin`、Hangfire 核心插件和 Redis 插件。适配器读取 `/Externals/Redis/ConnectionSettings`，优先使用名称为 `Hangfire` 且驱动器为 `Redis` 的连接设置；若不存在，则回退到默认设置或首个 Redis 设置。

请在启动 Hangfire 服务器前配置并验证 Redis 连接。所有 Hangfire 作业、状态、队列和服务器元数据都会通过该连接持久化。


## 安装与解析

```shell
dotnet add package Zongsoft.Externals.Hangfire.Storages.Redis
```

注册的 `RedisStorage` 在首次使用时延迟创建 StackExchange.Redis 连接。解析顺序是：驱动为 `Redis` 且名称精确为 `Hangfire` 的设置、默认 Redis 设置、首个 Redis 设置；均不可用时，首次访问存储会失败。

> 💡 建议使用专用 Redis Database/实例和显式 `Hangfire` 连接名，避免缓存淘汰或其他子系统静默影响后台作业。

🚨 Redis 是待处理作业的事实来源。请配置持久化、备份、内存策略、TLS/身份验证与高可用，绝不能采用会淘汰 Hangfire Key 的策略。连接是延迟创建的，因此启动健康检查必须主动访问。

投产前应在可丢弃实例中验证入队、处理、重试、定时作业、服务器重启与 Redis 故障转移。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../../Zongsoft.Plugins/README.zh-Hans.md)。

部署中保留 Hangfire 与框架 Redis 插件，存储选择使用框架 Redis 配置；运行任务前确认选中的逻辑数据库及独立键空间。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Hangfire.Storages.Redis` | [Zongsoft.Externals.Hangfire.Storages.Redis.plugin](Zongsoft.Externals.Hangfire.Storages.Redis.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Hangfire.Storages.Redis.deploy](Zongsoft.Externals.Hangfire.Storages.Redis.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals hangfire]
nuget:Zongsoft.Externals.Hangfire

[plugins zongsoft externals redis]
nuget:Zongsoft.Externals.Redis

[plugins zongsoft externals hangfire storages redis]
nuget:Zongsoft.Externals.Hangfire.Storages.Redis
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Hangfire.Storages.Redis.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

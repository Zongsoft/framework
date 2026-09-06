# Zongsoft.Externals.Hangfire.Web 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Hangfire.Web` 将 [Hangfire Dashboard](https://docs.hangfire.io/en/latest/configuration/using-dashboard.html) 集成到 Zongsoft ASP.NET Core 宿主中。

加载 `Zongsoft.Externals.Hangfire.Web.plugin` 后，插件会注册 Hangfire 服务，并在应用初始化阶段调用 `UseHangfireDashboard()`。因此，在配置好 `JobStorage` 后，宿主即可使用 Hangfire Dashboard 的默认路由。

该程序包仅提供 Web 宿主集成。调度和后台处理请使用 [Hangfire 核心插件](..)，并添加合适的存储程序包，例如 [Redis 适配器](../storages/redis)。请按照宿主应用的安全要求配置 Dashboard 授权和路由，切勿在没有访问控制的情况下将其暴露到公网。


## 安装与启动

```shell
dotnet add package Zongsoft.Externals.Hangfire.Web
```

服务注册调用 `AddHangfire`；应用初始化器调用无参数 `UseHangfireDashboard()`，因此使用 Hangfire 默认 Dashboard 路径和选项。中间件处理请求前必须注册有效全局 `JobStorage`。

> 💡 当前适配器没有提供修改 Dashboard 路径或 Filter 的选项。部署需要非默认 Dashboard 配置时，应直接定制宿主。

🚨 Hangfire Dashboard 默认授权行为不能替代应用安全审查。请验证远程访问规则、增加显式授权 Filter、防范 Cookie CSRF，并避免向未授权用户展示参数或异常详情。

请把 Dashboard 与所选存储和 Worker 一起测试：确认服务器心跳、队列/作业视图、重试/删除操作，以及未认证调用者被拒绝。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Plugins.Web 宿主，并部署下列依赖主适配器。服务/控制器发现发生在宿主初始化阶段，无需把业务逻辑放进 Program.cs；接收请求前验证端点及授权。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Hangfire.Web` | [Zongsoft.Externals.Hangfire.Web.plugin](Zongsoft.Externals.Hangfire.Web.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Hangfire.Web.deploy](Zongsoft.Externals.Hangfire.Web.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals hangfire]
nuget:Zongsoft.Externals.Hangfire

[plugins zongsoft externals hangfire web]
nuget:Zongsoft.Externals.Hangfire.Web
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Hangfire.Web.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

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

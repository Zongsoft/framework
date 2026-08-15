# Zongsoft.Externals.Wechat.Gateway 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Wechat.Gateway` 是 [Zongsoft 微信集成](..)的 ASP.NET Core 回调网关。它接收微信回调请求，并将请求正文及参数分派给具名的 Zongsoft `IHandler`。

## 路由与处理器

网关公开 `POST /Externals/Wechat/Fallback/{name}/{key?}` 路由。`{name}` 用于选择注册在 `FallbackExecutor.Instance.Handlers` 中的处理器，可选的键及全部请求参数通过执行上下文传入。处理器返回值作为响应内容，空返回值对应 `204 No Content`。

请同时加载 `Zongsoft.Externals.Wechat.Gateway.plugin` 及其依赖 `Zongsoft.Externals.Wechat`，并在 `/Workbench/Externals/Wechat/Fallback/Handlers` 下注册回调处理器。应根据所用微信服务的签名规范对回调端点实施保护和验证。

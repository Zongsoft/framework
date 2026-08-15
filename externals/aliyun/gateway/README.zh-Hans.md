# Zongsoft.Externals.Aliyun.Gateway 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Aliyun.Gateway` 是面向阿里云服务的 ASP.NET Core 回调网关。它接收服务回调，并将请求正文及参数分派给具名的 Zongsoft `IHandler`。

## 路由与处理器

网关公开 `POST /Externals/Aliyun/Fallback/{name}/{key?}` 路由。`{name}` 用于选择 `FallbackExecutor.Instance.Handlers` 中的处理器，可选的键及全部请求参数通过执行上下文传入。处理器返回值作为响应内容，空返回值对应 `204 No Content`。

请在 Zongsoft Web 宿主中加载 `Zongsoft.Externals.Aliyun.Gateway.plugin` 并注册所需的回调处理器，同时按照各阿里云服务的要求验证回调签名并限制端点访问。

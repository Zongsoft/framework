# Zongsoft.Externals.Wechat.Gateway Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Wechat.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Wechat.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Wechat.Gateway` is the ASP.NET Core callback gateway for the [Zongsoft WeChat integration](..). It receives callback requests from WeChat and dispatches each request body and its parameters to a named Zongsoft `IHandler`.

## Routing and Handlers

The gateway exposes `POST /Externals/Wechat/Fallback/{name}/{key?}`. The `{name}` segment selects a handler registered in `FallbackExecutor.Instance.Handlers`; the optional key and all request parameters are passed through the execution context. A handler result is returned as the response, while an empty result produces `204 No Content`.

Load `Zongsoft.Externals.Wechat.Gateway.plugin` together with its `Zongsoft.Externals.Wechat` dependency, then register callback handlers under `/Workbench/Externals/Wechat/Fallback/Handlers`. Callback endpoints should be protected and validated according to the signature requirements of the WeChat service being used.

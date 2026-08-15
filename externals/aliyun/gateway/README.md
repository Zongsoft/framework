# Zongsoft.Externals.Aliyun.Gateway Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Aliyun.Gateway)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Aliyun.Gateway)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Aliyun.Gateway` is an ASP.NET Core callback gateway for Alibaba Cloud services. It receives service callbacks and dispatches each request body and its parameters to a named Zongsoft `IHandler`.

## Routing and Handlers

The gateway exposes `POST /Externals/Aliyun/Fallback/{name}/{key?}`. The `{name}` segment selects a handler in `FallbackExecutor.Instance.Handlers`; the optional key and all request parameters are passed through the execution context. A handler result is returned as the response, while an empty result produces `204 No Content`.

Load `Zongsoft.Externals.Aliyun.Gateway.plugin` in a Zongsoft web host and register the required callback handlers. Validate callback signatures and restrict access according to the Alibaba Cloud service that owns each endpoint.

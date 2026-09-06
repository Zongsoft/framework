# Zongsoft.Externals.Hangfire.Samples 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Samples)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Samples)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

本项目是 [Zongsoft Hangfire 集成](..)的最小处理器示例。它定义了继承自 `HandlerBase<object>` 的 `MyHandler`，并以稳定名称 `MyHandler` 将其注册到 `/Workbench/Scheduler/Handlers`。

当 Hangfire 向该名称分派作业时，处理器会把参数、扩展参数和递增的执行次数写入 Zongsoft 诊断日志。运行示例时还需加载 Hangfire 核心插件、已配置的存储插件，并启动 Hangfire 服务器。

该示例只演示处理器注册；周期与延迟作业调度、服务器配置和存储设置请参阅[上级文档](..)。


## 构建、加载与验证

```shell
dotnet build externals/hangfire/samples/Zongsoft.Externals.Hangfire.Samples.csproj
```

在 Hangfire 核心插件之后部署范例程序集与插件。通过上级集成调度稳定名称 `MyHandler`，启动 Hangfire Server，并确认调试日志包含 `Count`、`Argument` 与 `Parameters`。

> 💡 计数器只是进程内演示状态；宿主重启会清零，多个 Worker 也不会共享同一计数。

🚨 请使用可丢弃存储和无害参数。Hangfire 会按策略重试失败作业，真实处理器必须幂等，不能假设只执行一次。

停止服务器并移除范例插件即可清理；测试作业应通过存储支持的管理流程删除。

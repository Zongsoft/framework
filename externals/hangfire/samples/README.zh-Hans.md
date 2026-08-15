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

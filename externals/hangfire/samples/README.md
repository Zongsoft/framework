# Zongsoft.Externals.Hangfire.Samples Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Samples)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Samples)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This project is the minimal handler sample for the [Zongsoft Hangfire integration](..). It defines `MyHandler`, derives it from `HandlerBase<object>`, and registers it under `/Workbench/Scheduler/Handlers` with the stable name `MyHandler`.

When Hangfire dispatches a job to that name, the handler writes the argument, parameters, and an incrementing execution count to the Zongsoft diagnostics log. Use the sample together with the core Hangfire plugin, a configured storage plugin, and a running Hangfire server.

The sample demonstrates handler registration only. Scheduling recurring and delayed jobs, server configuration, and storage setup are described in the [parent documentation](..).

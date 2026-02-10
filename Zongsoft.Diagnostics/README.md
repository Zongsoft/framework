# Zongsoft.Diagnostics 诊断插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## 概述

[**Z**ongsoft.**D**iagnosticss](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的诊断插件库，提供了基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 诊断遥测的功能集。

通过 [Zongsoft.Diagnosticsn.option](src/Zongsoft.Diagnostics.option) 配置文件默认定义了 _**O**pen**T**elemetry_ 标准的指标 _(Metric)_、跟踪 _(Trace)_ 导出器，以及 [_**P**rometheus_](https://prometheus.io) _指标_ 导出器 和 [_**Z**ipkin_](https://zipkin.io) _跟踪_ 导出器。

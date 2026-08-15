# Zongsoft.Externals.Grapecity 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**G**rapecity](https://github.com/Zongsoft/framework/tree/main/externals/grapecity) 将 [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) 适配到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的报表抽象中。

## 主要功能

- 为 ActiveReports 解析 Zongsoft 报表描述、数据源、参数和数据模型；
- 从框架的报表定位器及数据模型提供程序加载报表定义；
- 提供基于 ActiveReports 的报表设计器资源和主题集成；
- 可配合配套的 [Web 程序包](api)托管报表查看器与设计器端点。

请在 `Zongsoft.Reporting` 之后加载 `Zongsoft.Externals.Grapecity.plugin`。ActiveReports 是商业授权软件，请确保目标环境具备所需的运行时和设计器许可证。

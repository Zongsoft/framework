# Zongsoft.Externals.Grapecity.Web 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Grapecity.Web` 在 Zongsoft Web 应用中托管 [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) 查看器和设计器集成。它将 ActiveReports HTTP 服务连接到 `Zongsoft.Reporting` 与 `Zongsoft.Externals.Grapecity` 提供的报表定位器、模板、数据源和数据模型。

## Web 端点

该程序包提供报表列表、查看器报表定义，以及设计器资源、模板、缩略图、主题和数据模型等端点。报表发现入口为 `GET /Grapecity/Reporting/Reports`，模板端点位于 `/Grapecity/Reporting/Templates` 下。

请在核心 GrapeCity 与报表插件之后加载 `Zongsoft.Externals.Grapecity.Web.plugin`，并在公开查看器或设计器端点前，由宿主应用配置身份认证和授权。

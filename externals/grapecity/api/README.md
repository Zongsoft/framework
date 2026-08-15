# Zongsoft.Externals.Grapecity.Web Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity.Web)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity.Web)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Grapecity.Web` hosts the [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) viewer and designer integration for Zongsoft web applications. It connects ActiveReports HTTP services to the report locators, templates, data sources, and data models supplied by `Zongsoft.Reporting` and `Zongsoft.Externals.Grapecity`.

## Web Endpoints

The package provides endpoints for listing reports, serving report definitions to the viewer, and supplying designer resources, templates, thumbnails, themes, and data models. Report discovery starts at `GET /Grapecity/Reporting/Reports`, while template endpoints are rooted at `/Grapecity/Reporting/Templates`.

Load `Zongsoft.Externals.Grapecity.Web.plugin` after the core GrapeCity and reporting plugins. Configure authentication and authorization in the hosting application before exposing viewer or designer endpoints.

# Zongsoft.Externals.Grapecity Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Grapecity)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Grapecity)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**G**rapecity](https://github.com/Zongsoft/framework/tree/main/externals/grapecity) adapts [GrapeCity ActiveReports](https://developer.mescius.com/activereportsnet) to the reporting abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

## Features

- Resolves Zongsoft report descriptors, data sources, parameters, and data models for ActiveReports.
- Loads report definitions from the framework's report locators and data-model providers.
- Supplies designer resource and theme integration for ActiveReports-based report design.
- Works with the companion [web package](api) to host report viewer and designer endpoints.

Load `Zongsoft.Externals.Grapecity.plugin` after `Zongsoft.Reporting`. ActiveReports is commercially licensed software; ensure that the required runtime and designer licenses are available in the target environment.

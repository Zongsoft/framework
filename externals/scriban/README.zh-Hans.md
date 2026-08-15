# Zongsoft.Externals.Scriban 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Scriban)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Scriban)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**S**criban](https://github.com/Zongsoft/framework/tree/main/externals/scriban) 将 [Scriban](https://github.com/scriban/scriban) 模板语言集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的表达式服务中。

该插件将 `ScribanExpressionEvaluator` 注册为名为 `Scriban` 的 `IExpressionEvaluator`。应用可通过框架统一的求值器抽象执行 Scriban 表达式，并使用标准求值上下文传入变量。

加载 `Zongsoft.Externals.Scriban.plugin` 即可在宿主中使用该求值器。

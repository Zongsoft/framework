# Zongsoft.Externals.Scriban Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Scriban)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Scriban)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**S**criban](https://github.com/Zongsoft/framework/tree/main/externals/scriban) integrates the [Scriban](https://github.com/scriban/scriban) template language with the expression services of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

The plugin registers `ScribanExpressionEvaluator` as an `IExpressionEvaluator` named `Scriban`. Applications can evaluate Scriban expressions through the framework's common evaluator abstraction and pass variables through the standard evaluation context.

Load `Zongsoft.Externals.Scriban.plugin` to make the evaluator available to the host.

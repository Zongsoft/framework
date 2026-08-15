# Zongsoft.Externals.Lua Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Lua)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Lua)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**L**ua](https://github.com/Zongsoft/framework/tree/main/externals/lua) integrates the [Lua](https://www.lua.org/) language with the expression services of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. The implementation is built on `NLua` and `KeraLua`.

The plugin registers `LuaExpressionEvaluator` as an `IExpressionEvaluator` named `Lua`. It accepts variables from the standard evaluation context and makes the result available through the common evaluator API.

Load `Zongsoft.Externals.Lua.plugin` to make the evaluator available to the host. Evaluation examples are available in the [test project](test).

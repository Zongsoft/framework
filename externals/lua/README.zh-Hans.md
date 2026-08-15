# Zongsoft.Externals.Lua 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Lua)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Lua)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**L**ua](https://github.com/Zongsoft/framework/tree/main/externals/lua) 将 [Lua](https://www.lua.org/) 语言集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的表达式服务中，其实现基于 `NLua` 和 `KeraLua`。

该插件将 `LuaExpressionEvaluator` 注册为名为 `Lua` 的 `IExpressionEvaluator`。它接收标准求值上下文中的变量，并通过统一的求值器 API 返回执行结果。

加载 `Zongsoft.Externals.Lua.plugin` 即可在宿主中使用该求值器，具体求值示例可参考[测试项目](test)。

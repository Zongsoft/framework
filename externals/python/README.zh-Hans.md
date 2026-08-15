# Zongsoft.Externals.Python 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Python)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Python)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**P**ython](https://github.com/Zongsoft/framework/tree/main/externals/python) 将 [IronPython](https://ironpython.net/) 集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的表达式服务中。

该插件将 `PythonExpressionEvaluator` 注册为名为 `Python` 的 `IExpressionEvaluator`。它支持标准求值上下文中的变量、可配置的输入/输出/错误流、IronPython 标准库，以及用于 JSON 序列化和文本输出的辅助全局对象。

加载 `Zongsoft.Externals.Python.plugin` 即可在宿主中使用该求值器，具体求值示例可参考[测试项目](test)。

# Zongsoft.Externals.Python Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Python)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Python)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**P**ython](https://github.com/Zongsoft/framework/tree/main/externals/python) integrates [IronPython](https://ironpython.net/) with the expression services of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

The plugin registers `PythonExpressionEvaluator` as an `IExpressionEvaluator` named `Python`. It supports variables from the standard evaluation context, configurable input/output/error streams, the IronPython standard library, and helper globals for JSON serialization and text output.

Load `Zongsoft.Externals.Python.plugin` to make the evaluator available to the host. Evaluation examples are available in the [test project](test).

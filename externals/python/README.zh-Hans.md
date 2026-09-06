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


## 从插件宿主使用

先完成下文[插件化接入](#插件化接入)，再在已初始化的应用服务或命令中执行示例。消费项目只需引用 `Zongsoft.Core` 中的表达式契约，宿主部署语言实现；包引用本身不会加载插件。

```shell
dotnet add package Zongsoft.Core
```

```csharp
using Zongsoft.Expressions;
using Zongsoft.Services;

var evaluator = ApplicationContext.Current.Services
	.FindRequired<IExpressionEvaluator>("Python");
var variables = new Dictionary<string, object>
{
	["x"] = 20,
	["y"] = 22,
};

var result = evaluator.Evaluate("x + y", variables);
```

插件宿主可通过 `IExpressionEvaluator` 解析名为 `Python` 的求值器；独立工具和测试也可直接构造。求值器支持 IronPython 标准库以及 JSON 和输出辅助对象；语句脚本可把返回值放入 `result` 变量。

## 上下文、输出与生命周期

求值器是宿主注册的共享服务，消费方不要对其使用 `using` 或逐次 `Dispose()`。每次调用创建自己的变量字典；`Global` 用于共享函数和值，不宜在请求处理中修改。在自定义模块内可用 `Module.Current.Services` 做相同的按名称查找；也可以从应用选项读取求值器名，详见[服务定位与配置](../../Zongsoft.Core/README.zh-Hans.md)。

> 💡 求值结果是动态类型。请在应用边界转换或验证，不要在业务代码深处直接强制转换任意脚本结果。

🚨 当前 Python 实现复用一个 ScriptEngine，并在调用期间切换 Runtime 的输入/输出流后恢复；新建变量字典不等于运行时完全隔离。不要并发更改同一求值器的 IO 或全局状态；有隔离要求的应用应在自己拥有的执行边界串行化调用，或隔离运行进程。直接构造的独立求值器才由构造方负责释放。

## 安全与兼容性

🚨 脚本求值是代码执行，不是数据解析。切勿在高权限进程中执行不可信表达式。应限制暴露的委托和对象，在求值器外围约束时间/资源，并隔离确实需要处理恶意输入的工作负载。

语言值与 .NET 值并非完全对应：数字宽度、null/nil、字典、数组、成员大小写和异常都可能不同。请锁定适配器/运行时包版本，并测试应用拥有的每条表达式。语言行为参阅 [Python 运行时文档](https://ironpython.net/documentation/)。

## 延伸阅读

- [求值器测试](test)
- [外部适配器实现协作指南](../SKILL.md)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

服务扫描注册名为 `Python` 的 IExpressionEvaluator，部署还将 IronPython.StdLib 复制到 `lib`；仅保留求值器 DLL 可能导致导入不可用。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Python` | [Zongsoft.Externals.Python.plugin](src/Zongsoft.Externals.Python.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Python.deploy](src/Zongsoft.Externals.Python.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals python]
nuget:Zongsoft.Externals.Python
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Python.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

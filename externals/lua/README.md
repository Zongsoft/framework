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


## Use from a Plugin Host

Complete [plugin integration](#plugin-based-integration) below, then run this example inside an initialized application service or command. The consumer only references the expression contract in `Zongsoft.Core`; the host deploys the language implementation. A package reference alone does not load a plugin.

```shell
dotnet add package Zongsoft.Core
```

```csharp
using Zongsoft.Expressions;
using Zongsoft.Services;

var evaluator = ApplicationContext.Current.Services
	.FindRequired<IExpressionEvaluator>("Lua");
var variables = new Dictionary<string, object>
{
	["x"] = 20,
	["y"] = 22,
};

var result = evaluator.Evaluate("return x + y", variables);
```

Plugin hosts can resolve the named `Lua` evaluator through `IExpressionEvaluator`; direct construction is useful for isolated tools and tests. The evaluator exposes helpers for arrays, lists, dictionaries, JSON, `print`, and `error`, and converts Lua tables back to common .NET collections.

## Context, Output, and Lifetime

The evaluator is a shared host-registered service: consumers must not wrap it in `using` or call `Dispose()` per operation. Create a variables dictionary for each call. `Global` holds shared functions and values and should not be mutated during request handling. Within an application-defined module, use `Module.Current.Services` for the same named lookup. The evaluator name can also come from application options; see [service lookup and configuration](../../Zongsoft.Core/README.md).

> 💡 Treat evaluator results as dynamically typed. Convert or validate the result at the application boundary instead of casting arbitrary script output deep inside business code.

## Security and Compatibility

🚨 Script evaluation is code execution, not data parsing. Never evaluate untrusted expressions in a privileged process. Limit exposed delegates and objects, enforce time/resource limits outside the evaluator, and isolate workloads that require hostile-input support.

Language values do not map perfectly to .NET values: numeric width, null/nil, dictionaries, arrays, member casing, and exceptions can differ. Lock the adapter/runtime package versions and test every expression owned by the application. Consult the [Lua runtime documentation](https://www.lua.org/manual/5.4/) for language behavior.

## Related Resources

- [Evaluator tests](test)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Service scanning registers the `Lua` IExpressionEvaluator. The manifest additionally deploys platform/architecture-specific KeraLua native assets; supply both variables and verify the target architecture before resolving the evaluator.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Lua` | [Zongsoft.Externals.Lua.plugin](src/Zongsoft.Externals.Lua.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Lua.deploy](src/Zongsoft.Externals.Lua.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals lua]
nuget:Zongsoft.Externals.Lua
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Lua.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

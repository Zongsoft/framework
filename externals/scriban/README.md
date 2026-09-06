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


## Use from a Plugin Host

Complete [plugin integration](#plugin-based-integration) below, then run this example inside an initialized application service or command. The consumer only references the expression contract in `Zongsoft.Core`; the host deploys the language implementation. A package reference alone does not load a plugin.

```shell
dotnet add package Zongsoft.Core
```

```csharp
using Zongsoft.Expressions;
using Zongsoft.Services;

var evaluator = ApplicationContext.Current.Services
	.FindRequired<IExpressionEvaluator>("Scriban");
var variables = new Dictionary<string, object>
{
	["x"] = 20,
	["y"] = 22,
};

var result = evaluator.Evaluate("x + y", variables);
```

Plugin hosts can resolve the named `Scriban` evaluator through `IExpressionEvaluator`; direct construction is useful for isolated tools and tests. The evaluator parses Scriban in script-only mode, imports global and per-call variables case-insensitively, and returns the evaluated template value.

## Context, Output, and Lifetime

The evaluator is a shared host-registered service: consumers must not wrap it in `using` or call `Dispose()` per operation. Create a variables dictionary for each call. `Global` holds shared functions and values and should not be mutated during request handling. Within an application-defined module, use `Module.Current.Services` for the same named lookup. The evaluator name can also come from application options; see [service lookup and configuration](../../Zongsoft.Core/README.md).

> 💡 Treat evaluator results as dynamically typed. Convert or validate the result at the application boundary instead of casting arbitrary script output deep inside business code.

## Security and Compatibility

🚨 Script evaluation is code execution, not data parsing. Never evaluate untrusted expressions in a privileged process. Limit exposed delegates and objects, enforce time/resource limits outside the evaluator, and isolate workloads that require hostile-input support.

Language values do not map perfectly to .NET values: numeric width, null/nil, dictionaries, arrays, member casing, and exceptions can differ. Lock the adapter/runtime package versions and test every expression owned by the application. Consult the [Scriban runtime documentation](https://github.com/scriban/scriban/blob/master/site/docs/language.md) for language behavior.

## Related Resources

- [Evaluator implementation](src/ScribanExpressionEvaluator.cs)
- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

Service scanning registers the `Scriban` IExpressionEvaluator. Resolve that named service for application expressions and provide a non-null per-call variables dictionary; the current implementation dereferences its Count.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Scriban` | [Zongsoft.Externals.Scriban.plugin](src/Zongsoft.Externals.Scriban.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Scriban.deploy](src/Zongsoft.Externals.Scriban.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals scriban]
nuget:Zongsoft.Externals.Scriban
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Scriban.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

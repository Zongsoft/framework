# Zongsoft.Externals.Polly Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Polly)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Polly)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This plugin library provides a plugin-based adapter for the [**P**olly](https://github.com/App-vNext/Polly) open-source library and defines a unified abstraction and mapping for the related callback methods:

- `RetryFeature` ⇢ [`Polly.RetryStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/Retry)
- `TimeoutFeature` ⇢ [`Polly.TimeoutStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/Timeout)
- `BreakerFeature` ⇢ [`Polly.CircuitBreakerStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/CircuitBreaker)
- `ThrottleFeature` ⇢ `ThrottleStrategyOptions` _([overridden](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/ThrottleStrategy.cs))_
- `FallbackFeature` ⇢ `FallbackStrategyOptions` _([overridden](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/FallbackStrategy.cs))_

## Execution Modes

The [core library](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core) [executor](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Executor.cs) defines three execution modes based on the execution method's _argument_ and _return value_ types:

- **Synchronous** execution
	1. No argument _and_ no return value:
		- `IExecutor Build(Action execute);`
		- `IExecutor Build(Action<Parameters> execute);`
	2. Has argument _and_ no return value:
		- `IExecutor<TArgument> Build<TArgument>(Action<TArgument> execute);`
		- `IExecutor<TArgument> Build<TArgument>(Action<TArgument, Parameters> execute);`
	3. Has argument _and_ has return value:
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, TResult> execute);`
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, TResult> execute);`

- **Asynchronous** execution
	1. No argument _and_ no return value:
		- `IExecutor Build(Func<CancellationToken, ValueTask> execute);`
		- `IExecutor Build(Func<Parameters, CancellationToken, ValueTask> execute);`
	2. Has argument _and_ no return value:
		- `IExecutor<TArgument> Build<TArgument>(Func<TArgument, CancellationToken, ValueTask> execute);`
		- `IExecutor<TArgument> Build<TArgument>(Func<TArgument, Parameters, CancellationToken, ValueTask> execute);`
	3. Has argument _and_ has return value:
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, CancellationToken, ValueTask<TResult>> execute);`
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> execute);`

These three execution modes correspond to three [execution pipelines, `IFeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/IFeaturePipeline.cs). This plugin library's `FeaturePipeline` implementations, _([0](src/FeaturePipeline.cs), [1](src/FeaturePipeline`1.cs), [2](src/FeaturePipeline`2.cs))_, map to those three execution pipelines in order.

Because [**P**olly](https://github.com/App-vNext/Polly) _[version `8.6.5`](https://www.nuget.org/packages/Polly.Core/8.6.5)_ does not include the original execution argument in the corresponding strategy callbacks, the `Opened` callbacks of [`BreakerFeature<TArgument>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L95) and [`BreakerFeature<TArgument, TResult>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L125) cannot obtain the corresponding `BreakerOpenedArgument<TArgument>.Value` and `BreakerOpenedArgument<TArgument, TResult>.Value` values. The same limitation applies to their `Closed` callbacks.

This plugin library overrides the [throttling](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/ThrottleStrategy.cs) and [fallback](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/FallbackStrategy.cs) strategies, so their callback methods can access the original execution argument through the [`Argument<T>.Value`](../../Zongsoft.Core/src/Components/Features/Argument.cs) or [`Argument<T, TResult>.Value`](../../Zongsoft.Core/src/Components/Features/Argument.cs) property.

- [`ThrottleFeature<TArgument>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L91) and [`ThrottleFeature<TArgument, TResult>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L109)
- [`FallbackFeature<TArgument>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L76) and [`FallbackFeature<TArgument, TResult>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L98)

> 💡 **Tip:** when the `Rejected` callback method for throttling returns `true`, it means the rejection has been handled and the executor will not throw a throttling rejection exception.

## Notes

🚨 **Important:** the callback method signatures should match the signature mode of the execution method. Because of limitations in [**P**olly](https://github.com/App-vNext/Polly)'s underlying [`ResiliencePipelineBuilder`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.cs) and [`ResiliencePipelineBuilder<T>`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.TResult.cs), different [`IFeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/IFeaturePipeline.cs) modes cannot build incompatible [`Features`](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core/src/Components/Features). See these implementations for details:

- The constructor of [`FeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline.cs) and the `AddStrategy(ResiliencePipelineBuilder builder, IFeature feature)` method of [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs).
- The constructor of [`FeaturePipeline~1`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`1.cs) and the `AddStrategy<TArgument>(ResiliencePipelineBuilder builder, IFeature feature)` method of [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs).
- The constructor of [`FeaturePipeline~2`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`2.cs) and the `AddStrategy<TArgument, TResult>(ResiliencePipelineBuilder<TResult> builder, IFeature feature)` method of [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs).

> 💡 **Tip:** based on the implementation above, modes with return values are incompatible with both no-return modes, whether they have arguments or not. The no-argument and has-argument no-return modes are compatible with each other.

## Examples

See the [samples](https://github.com/Zongsoft/framework/tree/main/externals/polly/samples) [README.md](https://github.com/Zongsoft/framework/blob/main/externals/polly/samples/README.md) for examples.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest installs `FeaturePipelineBuilder.Instance` into the executor's Pipelines property at `/Workbench/Externals/Polly`. Configure resilience features through framework execution options; avoid separately overwriting that global binding.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Polly` | [Zongsoft.Externals.Polly.plugin](src/Zongsoft.Externals.Polly.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Polly.deploy](src/Zongsoft.Externals.Polly.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals polly]
nuget:Zongsoft.Externals.Polly
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Polly.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

# Zongsoft.Externals.Polly 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Polly)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Polly)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

该插件库基于 [**P**olly](https://github.com/App-vNext/Polly) 开源库的插件化适配，并针对相关回调方法做了统一的抽象和映射：

- `RetryFeature` ⇢ [`Polly.RetryStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/Retry)
- `TimeoutFeature` ⇢ [`Polly.TimeoutStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/Timeout)
- `BreakerFeature` ⇢ [`Polly.CircuitBreakerStrategyOptions`](https://github.com/App-vNext/Polly/tree/main/src/Polly.Core/CircuitBreaker)
- `ThrottleFeature` ⇢ `ThrottleStrategyOptions` _([重写](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/ThrottleStrategy.cs))_
- `FallbackFeature` ⇢ `FallbackStrategyOptions` _([重写](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/FallbackStrategy.cs))_

## 模式

[核心库](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core) 的 [执行器](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Executor.cs) 根据执行方法的 _参数_ 和 _返回值类型_ 定义了三种执行模式：

- **同步** 执行
	1. 无参 _且_ 无返回：
		- `IExecutor Build(Action execute);`
		- `IExecutor Build(Action<Parameters> execute);`
	2. 有参 _且_ 无返回：
		- `IExecutor<TArgument> Build<TArgument>(Action<TArgument> execute);`
		- `IExecutor<TArgument> Build<TArgument>(Action<TArgument, Parameters> execute);`
	3. 有参 _且_ 有返回：
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, TResult> execute);`
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, TResult> execute);`

- **异步** 执行
	1. 无参 _且_ 无返回：
		- `IExecutor Build(Func<CancellationToken, ValueTask> execute);`
		- `IExecutor Build(Func<Parameters, CancellationToken, ValueTask> execute);`
	2. 有参 _且_ 无返回：
		- `IExecutor<TArgument> Build<TArgument>(Func<TArgument, CancellationToken, ValueTask> execute);`
		- `IExecutor<TArgument> Build<TArgument>(Func<TArgument, Parameters, CancellationToken, ValueTask> execute);`
	3. 有参 _且_ 有返回：
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, CancellationToken, ValueTask<TResult>> execute);`
		- `IExecutor<TArgument, TResult> Build<TArgument, TResult>(Func<TArgument, Parameters, CancellationToken, ValueTask<TResult>> execute);`

以上三种执行模式分别对应了三种 [执行管线 `IFeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/IFeaturePipeline.cs)，本插件库的 `FeaturePipeline` _([0](src/FeaturePipeline.cs), [1](src/FeaturePipeline`1.cs), [2](src/FeaturePipeline`2.cs))_ 则依次对应了三种执行管线的实现。

由于 [**P**olly](https://github.com/App-vNext/Polly) 库 _[`8.6.5` 版本](https://www.nuget.org/packages/Polly.Core/8.6.5)_ 在相应策略回调中并没有包含原始执行参数，所以在 [`BreakerFeature<TArgument>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L95) 和 [`BreakerFeature<TArgument, TResult>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L125) 的 `Opened` 回调函数中的，是无法获得对应 `BreakerOpenedArgument<TArgument>.Value` 和 `BreakerOpenedArgument<TArgument, TResult>.Value` 属性值，其 `Closed` 回调函数亦同样如此。

由于本插件库重写了 [限流](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/ThrottleStrategy.cs) 和 [回退](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/FallbackStrategy.cs) 两种策略，因此可以在它们的回调方法中获取到原始执行参数的值，即通过 [`Argument<T>.Value`](../../Zongsoft.Core/src/Components/Features/Argument.cs) 或 [`Argument<T, TResult>.Value`](../../Zongsoft.Core/src/Components/Features/Argument.cs) 属性。

- [`ThrottleFeature<TArgument>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L91) 和 [`ThrottleFeature<TArgument, TResult>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L109)
- [`FallbackFeature<TArgument>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L76) 和 [`FallbackFeature<TArgument, TResult>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L98)

> 💡 **提示：** 限流的 `Rejected` 回调方法返回 `true`，表示已经处理完成，即执行器不会再抛出限流被拒绝异常。

## 其他说明

🚨 **注意：** 相关回调方法的签名应与执行方法的签名模式一致，因为 [**P**olly](https://github.com/App-vNext/Polly) 库底层的 [`ResiliencePipelineBuilder`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.cs) 与 [`ResiliencePipelineBuilder<T>`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.TResult.cs) 限制，因为如果不同模式的 [`IFeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/IFeaturePipeline.cs) 无法构建不兼容的 [`Features`](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core/src/Components/Features)，具体实现请参考：

- [`FeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br /> `AddStrategy(ResiliencePipelineBuilder builder, IFeature feature)` 方法。
- [`FeaturePipeline~1`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`1.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br /> `AddStrategy<TArgument>(ResiliencePipelineBuilder builder, IFeature feature)` 方法。
- [`FeaturePipeline~2`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`2.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br />`AddStrategy<TArgument, TResult>(ResiliencePipelineBuilder<TResult> builder, IFeature feature)` 方法。

> 💡 **提示：** 根据上述实现，可观察到带返回值的模式不兼容无返回值的两种执行模式 _(有参或无参)_；但无返回值模式中的无参和有参两种执行模式则彼此兼容。

## 使用范例

请参考本插件库的 [samples](https://github.com/Zongsoft/framework/tree/main/externals/polly/samples) 范例的 [README.md](https://github.com/Zongsoft/framework/blob/main/externals/polly/samples/README.md) 文档。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单通过 `/Workbench/Externals/Polly` 将 `FeaturePipelineBuilder.Instance` 绑定到执行器的 Pipelines 属性。通过框架执行选项配置韧性特性，避免另行覆盖该全局绑定。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Polly` | [Zongsoft.Externals.Polly.plugin](src/Zongsoft.Externals.Polly.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Polly.deploy](src/Zongsoft.Externals.Polly.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals polly]
nuget:Zongsoft.Externals.Polly
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Polly.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

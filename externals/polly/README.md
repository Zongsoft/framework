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

由于 [**P**olly](https://github.com/App-vNext/Polly) 库 _(version 8.6.5)_ 在相应策略回调中并没有包含原始执行参数，所以在 [`BreakerFeature<TArgument>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L95) 和 [`BreakerFeature<TArgument, TResult>`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/BreakerFeature.cs#L125) 的 `Opened` 回调函数中的，是无法获得对应 `BreakerOpenedArgument<TArgument>.Value` 和 `BreakerOpenedArgument<TArgument, TResult>.Value` 属性值，其 `Closed` 回调函数亦同样如此。

由于本插件库重写了 [限流](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/ThrottleStrategy.cs) 和 [回退](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/Strategies/FallbackStrategy.cs) 两种策略，因此可以在它们的回调方法中获取到原始执行参数的值，即通过 [`Argument<T>.Value`](https://github.com/Zongsoft/framework/blob/execution/Zongsoft.Core/src/Components/Features/Argument.cs#L60) 或 [`Argument<T, TResult>.Value`](https://github.com/Zongsoft/framework/blob/execution/Zongsoft.Core/src/Components/Features/Argument.cs#L60) 属性。

- [`ThrottleFeature<TArgument>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L91) 和 [`ThrottleFeature<TArgument, TResult>.Rejected`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/ThrottleFeature.cs#L109)
- [`FallbackFeature<TArgument>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L76) 和 [`FallbackFeature<TArgument, TResult>.Fallback`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/Features/FallbackFeature.cs#L98)

> 💡 **提示：** 限流的 `Rejected` 回调方法返回 `true`，表示已经处理完成，即执行器不会再抛出限流被拒绝异常。

## 其他说明

🚨 **注意：** 相关回调方法的签名应与执行方法的签名模式一致，因为 [**P**olly](https://github.com/App-vNext/Polly) 库底层的 [`ResiliencePipelineBuilder`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.cs) 与 [`ResiliencePipelineBuilder<T>`](https://github.com/App-vNext/Polly/blob/main/src/Polly.Core/ResiliencePipelineBuilder.TResult.cs) 限制，因为如果不同模式的 [`IFeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/Zongsoft.Core/src/Components/IFeaturePipeline.cs) 无法构建不兼容的 [`Features`](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Core/src/Components/Features)，具体实现请参考：

- [`FeaturePipeline`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br /> `AddStrategy(ResiliencePipelineBuilder builder, IFeature feature)` 方法。
- [`FeaturePipeline~1`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`1.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br /> `AddStrategy<TArgument>(ResiliencePipelineBuilder builder, IFeature feature)` 方法。
- [`FeaturePipeline~2`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeaturePipeline`2.cs) 类的构造函数 与 [`FeatureExtension`](https://github.com/Zongsoft/framework/blob/main/externals/polly/src/FeatureExtension.cs) 类的 <br />`AddStrategy<TArgument, TResult>(ResiliencePipelineBuilder<TResult> builder, IFeature feature)` 方法。

> 💡 **提示：** 根据上述实现，可观察到带返回值的执行模式不兼容无返回值的两种执行模式 _(有参或无参)_；但无返回值的模式中的无参和有参两种模式彼此兼容。

## 使用范例

请参考本插件库的 [samples](https://github.com/Zongsoft/framework/tree/main/externals/polly/samples) 范例的 [README.md](https://github.com/Zongsoft/framework/blob/main/externals/polly/samples/README.md) 文档。

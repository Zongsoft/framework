using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Components.Features;

namespace Zongsoft.Externals.Polly.Tests;

[CollectionDefinition(nameof(EventDispatchCollection), DisableParallelization = true)]
public sealed class EventDispatchCollection;

[Collection(nameof(EventDispatchCollection))]
public class EventDispatchTest
{
	[Fact]
	public async Task DefaultExecutorsDoNotConsultPipelineBuilder()
	{
		var original = Executor.Pipelines;
		var builder = new RecordingBuilder();
		Executor.Pipelines = builder;

		try
		{
			var count = 0;
			await Executor.Build<int>(_ => count++).ExecuteAsync(1);
			var value = await Executor.Build<int, int>(value => value + 1).ExecuteAsync(2);
			await using var dispatcher = new EventDispatcher();
			await dispatcher.StartAsync();
			await dispatcher.DispatchAsync(new EventContext<int>(new Registry(), "Changed", 1), Executor.Build<EventContext>(_ => count++));

			Assert.Equal(2, count);
			Assert.Equal(3, value);
			Assert.Equal(0, builder.Builds);
		}
		finally { Executor.Pipelines = original; }
	}

	[Fact]
	public async Task ExplicitRetryRunsAfterEnvironmentAllowsDelivery()
	{
		var original = Executor.Pipelines;
		var builder = new RecordingBuilder();
		Executor.Pipelines = builder;

		try
		{
			var ready = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			var feature = new Condition(cancellation => new(ready.Task.WaitAsync(cancellation)));
			await using var dispatcher = new EventDispatcher(new Provider(feature))
			{
				Pipeline = Executor.Pipelines.Build<EventContext>(Executor.Features.Retry(TimeSpan.Zero, false, 1).Build()),
			};

			await dispatcher.StartAsync();

			var attempts = 0;
			var executor = Executor.Build<EventContext>((Action<EventContext>)(_ =>
			{
				if(++attempts == 1)
					throw new InvalidOperationException("Retry this attempt.");
			}));

			await dispatcher.DispatchAsync(new EventContext<int>(new Registry(), "Changed", 1), executor);

			Assert.Equal(0, attempts);
			Assert.IsType<RetryFeature>(Assert.Single(builder.Features));

			ready.SetResult(true);
			await dispatcher.StopAsync();

			Assert.Equal(2, attempts);
			Assert.Equal(1, builder.Builds);
		}
		finally { Executor.Pipelines = original; }
	}

	[Fact]
	public async Task ExplicitTimeoutAndFallbackPreserveFailure()
	{
		var original = Executor.Pipelines;
		Executor.Pipelines = FeaturePipelineBuilder.Instance;

		try
		{
			var fallback = 0;
			var features = Executor.Features.Fallback((_, _) =>
			{
				fallback++;
				return ValueTask.CompletedTask;
			}).Timeout(TimeSpan.FromMilliseconds(50));

			var executor = Executor.Build<EventContext>(async (_, cancellation) => await Task.Delay(Timeout.Infinite, cancellation));
			await using var dispatcher = new EventDispatcher()
			{
				Pipeline = Executor.Pipelines.Build<EventContext>(features.Build()),
			};

			await dispatcher.StartAsync();
			await Assert.ThrowsAsync<global::Polly.Timeout.TimeoutRejectedException>(() =>
				dispatcher.DispatchAsync(new EventContext<int>(new Registry(), "Changed", 1), executor).AsTask());

			Assert.Equal(1, fallback);
		}
		finally { Executor.Pipelines = original; }
	}

	private sealed class Registry : EventRegistryBase
	{
		public Registry() : base("PollyDispatchTests") => this.Event<int>("Changed");
	}

	private sealed class Condition(Func<CancellationToken, ValueTask<bool>> predicate) : IFeature, IPredication
	{
		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
		public ValueTask<bool> PredicateAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => predicate(cancellation);
	}

	private sealed class Provider(IFeature feature) : IFeatureProvider
	{
		public IEnumerable<IFeature> GetFeatures() => [feature];
	}

	private sealed class RecordingBuilder : IFeaturePipelineBuilder
	{
		public int Builds { get; private set; }
		public IFeature[] Features { get; private set; }
		public IFeaturePipeline Build(IEnumerable<IFeature> features) => throw new NotSupportedException();
		public IFeaturePipeline<TArgument> Build<TArgument>(IEnumerable<IFeature> features)
		{
			this.Builds++;
			this.Features = features.ToArray();
			return FeaturePipelineBuilder.Instance.Build<TArgument>(this.Features);
		}
		public IFeaturePipeline<TArgument, TResult> Build<TArgument, TResult>(IEnumerable<IFeature> features) => throw new NotSupportedException();
	}
}

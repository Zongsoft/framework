using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[Collection(Data.Transactions.Tests.TransactionEventDispatchCollection.Name)]
public class EventDispatcherTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PredicateReceivesEventAndParametersAndControlsDelivery(bool allow)
	{
		var argument = Context(4);
		argument.Parameters["allowed"] = allow;
		var calls = 0;
		var feature = new Condition((value, parameters, cancellation) =>
		{
			calls++;
			Assert.Same(argument, value);
			Assert.Same(argument.Parameters, parameters);
			return ValueTask.FromResult((bool)parameters["allowed"]);
		});
		var pipeline = new ProbePipeline();
		await using var dispatcher = new EventDispatcher(new Provider(feature, feature)) { Pipeline = pipeline };
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(argument, executor);

		Assert.Equal(1, calls);
		Assert.Equal(allow ? 1 : 0, pipeline.Calls);
		Assert.Equal(allow ? 1 : 0, executor.Contexts.Count);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task PipelineRunsOnlyAfterEnvironmentAllowsDelivery(bool allow)
	{
		var gate = NewGate();
		var feature = Bind(gate);
		var pipeline = new ProbePipeline();
		await using var dispatcher = new EventDispatcher(new Provider(feature)) { Pipeline = pipeline };
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(7), executor);

		Assert.Equal(0, pipeline.Calls);
		gate.SetResult(allow);
		await dispatcher.StopAsync();

		Assert.Equal(allow ? 1 : 0, pipeline.Calls);

		if(allow)
		{
			var context = Assert.Single(executor.Contexts);
			Assert.Equal(7, Value(context.Argument));
			Assert.Same(feature, Assert.Single(context.Features));
			Assert.Same(pipeline, context.Parameters["pipeline"]);
		}
		else
			Assert.Empty(executor.Contexts);
	}

	[Fact]
	public async Task PipelineReplaysExecutorWithCapturedFeaturesParametersAndCancellation()
	{
		using var cancellation = new CancellationTokenSource();
		using var execution = new CancellationTokenSource();
		var provider = new Provider(new Condition((_, _, _) => ValueTask.FromResult(true)));
		var pipeline = new ProbePipeline
		{
			Executing = async (next, argument, parameters, token) =>
			{
				Assert.Equal(cancellation.Token, token);
				await next(argument, parameters, execution.Token);
				await next(argument, parameters, execution.Token);
			},
		};
		await using var dispatcher = new EventDispatcher(provider) { Pipeline = pipeline };
		var executor = new ProbeExecutor();
		var filter = new ProbeFilter();
		executor.Filters.Add(filter);
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(3), executor, cancellation.Token);

		Assert.Equal(1, pipeline.Calls);
		Assert.Equal(1, provider.Reads);
		Assert.Equal(["before", "after", "before", "after"], filter.Calls);
		var contexts = executor.Contexts.ToArray();
		Assert.Equal(2, contexts.Length);
		Assert.NotSame(contexts[0], contexts[1]);
		Assert.Same(contexts[0].Argument, contexts[1].Argument);
		Assert.Same(contexts[0].Parameters, contexts[1].Parameters);
		Assert.All(contexts, context =>
		{
			Assert.Same(provider.Features[0], Assert.Single(context.Features));
			Assert.Same(pipeline, context.Parameters["pipeline"]);
		});
		Assert.Equal([execution.Token, execution.Token], executor.Tokens);
	}

	[Fact]
	public async Task PipelineIsReusedAndCanOnlyBeReplacedAfterStop()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var pipeline = new ProbePipeline();
		var replacement = new ProbePipeline();
		await using var dispatcher = new EventDispatcher(new FeatureProvider()) { Pipeline = pipeline };
		var executor = new ProbeExecutor(() => new(release.Task));
		await dispatcher.StartAsync();
		var pending = dispatcher.DispatchAsync(Context(1), executor).AsTask();
		await executor.Received.WaitAsync(TimeSpan.FromSeconds(10));

		try
		{
			Assert.Throws<InvalidOperationException>(() => dispatcher.Pipeline = replacement);
			var stopping = dispatcher.StopAsync();
			Assert.False(stopping.IsCompleted);
			Assert.Throws<InvalidOperationException>(() => dispatcher.Pipeline = null);
			release.SetResult();
			await Task.WhenAll(pending, stopping).WaitAsync(TimeSpan.FromSeconds(10));
		}
		finally { release.TrySetResult(); }

		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(2), executor);
		await dispatcher.StopAsync();
		Assert.Equal(2, pipeline.Calls);
		Assert.Same(pipeline, dispatcher.Pipeline);

		dispatcher.Pipeline = replacement;
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(3), executor);
		await dispatcher.StopAsync();
		Assert.Equal(2, pipeline.Calls);
		Assert.Equal(1, replacement.Calls);

		dispatcher.Pipeline = null;
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(4), executor);
		await dispatcher.StopAsync();
		dispatcher.Pipeline = replacement;
		await dispatcher.DisposeAsync();
		Assert.Equal(4, executor.Contexts.Count);
		Assert.Equal(1, replacement.Calls);
		Assert.False(pipeline.Disposed);
		Assert.False(replacement.Disposed);
	}

	[Fact]
	public async Task GlobalAndExplicitProvidersAreCombinedByIdentity()
	{
		var shared = new Provider(new Condition((_, _, _) => ValueTask.FromResult(true)));
		var other = new Provider(new Condition((_, _, _) => ValueTask.FromResult(true)));
		await using var dispatcher = new EventDispatcher(new FeatureProvider(FeatureProvider.Default, shared, shared, other));
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		FeatureProvider.Default.Providers.Add(shared);

		try
		{
			await dispatcher.DispatchAsync(Context(1), executor);

			Assert.Equal(1, shared.Reads);
			Assert.Equal(1, other.Reads);
			var features = Assert.Single(executor.Contexts).Features;
			Assert.Equal(2, features.Count);
			Assert.Contains(shared.Features[0], features);
			Assert.Contains(other.Features[0], features);
		}
		finally { FeatureProvider.Default.Providers.Remove(shared); }
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ComposingWithDefaultPreservesTransactionCondition(bool commit)
	{
		var provider = new Provider(new Condition((_, _, _) => ValueTask.FromResult(true)));
		await using var dispatcher = new EventDispatcher(new FeatureProvider(FeatureProvider.Default, provider));
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();

		await using(var transaction = new Data.Transaction())
		{
			await dispatcher.DispatchAsync(Context(1), executor);

			Assert.Empty(executor.Contexts);
			Assert.Equal(1, provider.Reads);

			if(commit)
				await transaction.CommitAsync();
			else
				await transaction.RollbackAsync();
		}

		await dispatcher.StopAsync();

		if(commit)
		{
			var context = Assert.Single(executor.Contexts);
			Assert.Equal(2, context.Features.Count);
			Assert.Contains(provider.Features[0], context.Features);
		}
		else
			Assert.Empty(executor.Contexts);
	}

	[Fact]
	public async Task EnvironmentSnapshotPrecedesProviderCallbacks()
	{
		var second = new Provider();
		var third = new Provider();
		var first = new Provider
		{
			Capturing = () =>
			{
				if(FeatureProvider.Default.Providers.Remove(second))
					FeatureProvider.Default.Providers.Add(third);
			},
		};
		await using var dispatcher = new EventDispatcher();
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		FeatureProvider.Default.Providers.Add(first);
		FeatureProvider.Default.Providers.Add(second);

		try
		{
			await dispatcher.DispatchAsync(Context(1), executor);

			Assert.Equal(1, first.Reads);
			Assert.Equal(1, second.Reads);
			Assert.Equal(0, third.Reads);

			await dispatcher.DispatchAsync(Context(2), executor);

			Assert.Equal(2, first.Reads);
			Assert.Equal(1, second.Reads);
			Assert.Equal(1, third.Reads);
			Assert.Equal(2, executor.Contexts.Count);
		}
		finally
		{
			FeatureProvider.Default.Providers.Remove(first);
			FeatureProvider.Default.Providers.Remove(second);
			FeatureProvider.Default.Providers.Remove(third);
		}
	}

	[Fact]
	public async Task CompositeCopiesConstructorSources()
	{
		var first = new Provider();
		var second = new Provider();
		IFeatureProvider[] providers = [first];
		await using var dispatcher = new EventDispatcher(new FeatureProvider(providers));
		providers[0] = second;
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(Context(1), new ProbeExecutor());

		Assert.Equal(1, first.Reads);
		Assert.Equal(0, second.Reads);
	}

	[Fact]
	public async Task ExplicitProviderOwnsAllConditions()
	{
		await using var dispatcher = new EventDispatcher(new FeatureProvider());
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		await using var transaction = new Data.Transaction();

		await dispatcher.DispatchAsync(Context(1), executor);

		Assert.Empty(Assert.Single(executor.Contexts).Features);
		Assert.Equal(Data.Transactions.TransactionStatus.Active, transaction.Context.Status);
		await transaction.RollbackAsync();
	}

	[Fact]
	public async Task DeniedConditionDoesNotWaitForUnresolvedCondition()
	{
		var pending = NewGate();
		var denied = NewGate();
		denied.SetResult(false);
		var provider = new Provider(Bind(pending), Bind(denied));
		await using var dispatcher = new EventDispatcher(provider, 1);
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(Context(1), executor).AsTask().WaitAsync(TimeSpan.FromSeconds(10));
		provider.Features = [];
		await dispatcher.DispatchAsync(Context(2), executor);

		Assert.False(pending.Task.IsCompleted);
		Assert.Equal(2, Value(Assert.Single(executor.Contexts).Argument));
	}

	[Fact]
	public async Task CapturesFeaturesOnceAndRunsExecutorFilters()
	{
		var gate = NewGate();
		var feature = Bind(gate);
		var provider = new Provider(feature);
		await using var dispatcher = new EventDispatcher(new FeatureProvider(provider, provider));
		var executor = new ProbeExecutor();
		var filter = new ProbeFilter();
		executor.Filters.Add(filter);
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(Context(1), executor);
		provider.Features = [];
		gate.SetResult(true);
		await dispatcher.StopAsync();

		Assert.Equal(1, provider.Reads);
		Assert.Same(feature, Assert.Single(Assert.Single(executor.Contexts).Features));
		Assert.Equal(["before", "after"], filter.Calls);
	}

	[Fact]
	public async Task UnreadyEnvironmentDoesNotBlockReadyEnvironment()
	{
		var first = NewGate();
		var second = NewGate();
		var provider = new Provider(Bind(first));
		await using var dispatcher = new EventDispatcher(provider);
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(Context(1), executor);
		provider.Features = [Bind(second)];
		await dispatcher.DispatchAsync(Context(2), executor);
		second.SetResult(true);
		await executor.Received.WaitAsync(TimeSpan.FromSeconds(10));

		Assert.Equal(2, Value(Assert.Single(executor.Contexts).Argument));

		first.SetResult(true);
		await dispatcher.StopAsync();

		Assert.Equal([2, 1], executor.Contexts.Select(context => Value(context.Argument)));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task CombinesAllConditions(bool allow)
	{
		var first = NewGate();
		var second = NewGate();
		await using var dispatcher = new EventDispatcher(new Provider(Bind(first), Bind(second)));
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();

		await dispatcher.DispatchAsync(Context(1), executor);
		first.SetResult(true);

		Assert.Empty(executor.Contexts);

		second.SetResult(allow);
		await dispatcher.StopAsync();

		Assert.Equal(allow ? 1 : 0, executor.Contexts.Count);
	}

	[Fact]
	public async Task RejectsFullAdmissionWithoutWaitingForEnvironment()
	{
		var gate = NewGate();
		await using var dispatcher = new EventDispatcher(new Provider(Bind(gate)), 1);
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(1), executor);

		await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(Context(2), executor).AsTask());
		await dispatcher.StopAsync();
		gate.SetResult(true);
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(3), executor);
		await dispatcher.StopAsync();

		Assert.Equal(3, Value(Assert.Single(executor.Contexts).Argument));
	}

	[Fact]
	public async Task StopDrainsReadyDeliveryBeforeCompleting()
	{
		var gate = NewGate();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var dispatcher = new EventDispatcher(new Provider(Bind(gate)));
		var executor = new ProbeExecutor(() => new(release.Task));
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(1), executor);
		gate.SetResult(true);
		await executor.Received.WaitAsync(TimeSpan.FromSeconds(10));

		var stopping = dispatcher.StopAsync();
		Assert.False(stopping.IsCompleted);

		release.SetResult();
		await stopping.WaitAsync(TimeSpan.FromSeconds(10));
	}

	[Fact]
	public async Task CancellationDoesNotChangeSharedEnvironment()
	{
		var gate = NewGate();
		var tokens = new List<CancellationToken>();
		var feature = new Condition((_, _, cancellation) =>
		{
			tokens.Add(cancellation);
			return new(gate.Task.WaitAsync(cancellation));
		});
		await using var dispatcher = new EventDispatcher(new Provider(feature));
		using var cancellation = new CancellationTokenSource();
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(Context(1), executor, cancellation.Token);
		cancellation.Cancel();
		await dispatcher.DispatchAsync(Context(2), executor);

		Assert.Equal(2, tokens.Count);
		Assert.False(tokens[0].IsCancellationRequested);
		Assert.False(tokens[1].IsCancellationRequested);
		Assert.False(gate.Task.IsCompleted);
		gate.SetResult(true);
		await dispatcher.StopAsync();

		Assert.True(await gate.Task);
		Assert.Equal(2, Value(Assert.Single(executor.Contexts).Argument));
	}

	[Fact]
	public async Task UnknownRuntimeFeatureFailsBeforeAdmission()
	{
		await using var dispatcher = new EventDispatcher(new Provider(new UnknownFeature()));
		await dispatcher.StartAsync();

		await Assert.ThrowsAsync<NotSupportedException>(() => dispatcher.DispatchAsync(Context(1), new ProbeExecutor()).AsTask());
		await dispatcher.StopAsync();
	}

	[Fact]
	public async Task ImmediateFailurePropagatesAndDeferredFailureDoesNotBreakFollowingWork()
	{
		var gate = NewGate();
		var provider = new Provider();
		await using var dispatcher = new EventDispatcher(provider);
		var failed = Executor.Build<EventContext>((Action<EventContext>)(_ => throw new InvalidOperationException("Delivery failure")));
		var next = new ProbeExecutor();
		await dispatcher.StartAsync();

		await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(Context(0), failed).AsTask());
		provider.Features = [Bind(gate)];
		await dispatcher.DispatchAsync(Context(1), failed);
		await dispatcher.DispatchAsync(Context(2), next);
		gate.SetResult(true);
		await dispatcher.StopAsync();

		Assert.Equal(2, Value(Assert.Single(next.Contexts).Argument));
	}

	[Fact]
	public void ContextCopiesFeatureCollection()
	{
		var feature = new UnknownFeature();
		IFeature[] features = [feature];
		var context = new ExecutorContext<int>(Executor.Build<int>(_ => { }), 1, new Parameters(), features);
		features[0] = null;

		Assert.Same(feature, Assert.Single(context.Features));
		Assert.Throws<NotSupportedException>(() => ((IList<IFeature>)context.Features).Clear());
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DeferredSnapshotPreservesDeclaredCollectionType(bool empty)
	{
		var gate = NewGate();
		await using var dispatcher = new EventDispatcher(new Provider(Bind(gate)));
		var registry = new Registry([typeof(IEnumerable<string>)]);
		var values = new List<string> { "before" };
		var context = new EventContext<IEnumerable<string>>(registry, "Changed", empty ? null : values);
		context.Parameters["source"] = "before";
		var executor = new ProbeExecutor();
		await dispatcher.StartAsync();
		await dispatcher.DispatchAsync(context, executor);
		values.Add("after");
		context.Parameters["source"] = "after";
		gate.SetResult(true);
		await dispatcher.StopAsync();

		var delivered = Assert.IsType<EventContext<IEnumerable<string>>>(Assert.Single(executor.Contexts).Argument);
		Assert.Same(registry["Changed"], delivered.Descriptor);
		Assert.Equal(typeof(IEnumerable<string>), delivered.ArgumentType);
		Assert.Equal("before", delivered.Parameters["source"]);

		if(empty)
			Assert.Null(delivered.Argument);
		else
			Assert.Equal(["before"], delivered.Argument);
	}

	private static TaskCompletionSource<bool> NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);
	private static IFeature Bind(TaskCompletionSource<bool> gate) => new Condition((_, _, cancellation) => new(gate.Task.WaitAsync(cancellation)));
	private static EventContext Context(int value) => new EventContext<int>(new Registry(), "Changed", value);
	private static int Value(EventContext context) => context is EventContext<int> typed ? typed.Argument : (int)((EventContext<object>)context).Argument;

	private sealed class Registry : EventRegistryBase
	{
		public Registry() : this([typeof(int)]) { }
		public Registry(Type[] types) : base("DispatchTests") => this.Event(new EventDescriptor("Changed", types));
	}

	private sealed class UnknownFeature : IFeature;

	private sealed class Condition(Func<object, Parameters, CancellationToken, ValueTask<bool>> predicate) : IFeature, IPredication
	{
		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
		public ValueTask<bool> PredicateAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => predicate(argument, parameters, cancellation);
	}

	private sealed class Provider(params IFeature[] features) : IFeatureProvider
	{
		public IFeature[] Features { get; set; } = features;
		public int Reads { get; private set; }
		public Action Capturing { get; init; }
		public IEnumerable<IFeature> GetFeatures()
		{
			this.Reads++;
			this.Capturing?.Invoke();
			return this.Features;
		}
	}

	private sealed class ProbeExecutor(Func<ValueTask> execute = null) : ExecutorBase<EventContext>
	{
		public ConcurrentQueue<IExecutorContext<EventContext>> Contexts { get; } = new();
		public ConcurrentQueue<CancellationToken> Tokens { get; } = new();
		public TaskCompletionSource ReceivedSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Task Received => this.ReceivedSource.Task;
		protected override IExecutorContext<EventContext> CreateContext(EventContext argument, Parameters parameters) => new ExecutorContext<EventContext>(this, argument, parameters);
		protected override ValueTask OnExecuteAsync(IExecutorContext<EventContext> context, CancellationToken cancellation = default)
		{
			this.Contexts.Enqueue(context);
			this.Tokens.Enqueue(cancellation);
			this.ReceivedSource.TrySetResult();
			return execute?.Invoke() ?? ValueTask.CompletedTask;
		}
	}

	private sealed class ProbeFilter : IFilter<IExecutorContext>
	{
		public List<string> Calls { get; } = [];
		public ValueTask OnFiltering(IExecutorContext context, CancellationToken cancellation)
		{
			this.Calls.Add("before");
			return ValueTask.CompletedTask;
		}
		public ValueTask OnFiltered(IExecutorContext context, CancellationToken cancellation)
		{
			this.Calls.Add("after");
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ProbePipeline : IFeaturePipeline<EventContext>, IDisposable
	{
		public int Calls { get; private set; }
		public bool Disposed { get; private set; }
		public IEnumerable<IFeature> Features => [];
		public Func<Func<EventContext, Parameters, CancellationToken, ValueTask>, EventContext, Parameters, CancellationToken, ValueTask> Executing { get; init; }
		public void Execute(Action<EventContext> executor, EventContext argument) => throw new NotSupportedException();
		public void Execute(Action<EventContext, Parameters> executor, EventContext argument, Parameters parameters) => throw new NotSupportedException();
		public ValueTask ExecuteAsync(Func<EventContext, CancellationToken, ValueTask> executor, EventContext argument, CancellationToken cancellation = default) =>
			this.ExecuteAsync((context, _, token) => executor(context, token), argument, new(), cancellation);
		public ValueTask ExecuteAsync(Func<EventContext, Parameters, CancellationToken, ValueTask> executor, EventContext argument, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Calls++;
			parameters["pipeline"] = this;
			return this.Executing == null ? executor(argument, parameters, cancellation) : this.Executing(executor, argument, parameters, cancellation);
		}
		public void Dispose() => this.Disposed = true;
	}
}

using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

public class EventDescriptorAsyncTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task HandleAsync_PendingPredicate_ReturnsBeforePredicateCompletes(bool typed, bool matches)
	{
		using var cancellation = new CancellationTokenSource();
		var predicate = new Predicate();
		var handler = new Handler();
		var descriptor = CreateDescriptor(typed, predicate, handler);
		var parameters = Parameters.Parameter("number", 42);
		var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() => returned.SetResult(Invoke(descriptor, typed, parameters, cancellation.Token).AsTask()));

		try
		{
			var handling = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(handling.IsCompleted);
			Assert.Equal(0, handler.CallCount);
			Assert.Equal("argument", predicate.Argument);
			Assert.Same(parameters, predicate.Parameters);
			Assert.Equal(cancellation.Token, predicate.Cancellation);

			predicate.Completion.SetResult(matches);
			await handling.WaitAsync(TimeSpan.FromSeconds(5));

			Assert.Equal(matches ? 1 : 0, handler.CallCount);
			if(matches)
			{
				Assert.Equal("argument", handler.Argument);
				Assert.Same(parameters, handler.Parameters);
				Assert.Equal(cancellation.Token, handler.Cancellation);
				Assert.True(handler.Typed);
			}
		}
		finally
		{
			predicate.Completion.TrySetResult(false);
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task HandleAsync_SynchronousHandlerFailure_StillAwaitsOtherHandlers(bool typed)
	{
		var failure = new InvalidOperationException("handler failed");
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failed = new Handler(() => throw failure);
		var pending = new Handler(() => new ValueTask(completion.Task));
		var completed = new Handler();
		var descriptor = CreateDescriptor(typed, null, failed, pending, completed);
		var handling = Invoke(descriptor, typed).AsTask();

		try
		{
			Assert.Equal(1, failed.CallCount);
			Assert.Equal(1, pending.CallCount);
			Assert.Equal(1, completed.CallCount);
			Assert.False(handling.IsCompleted);
		}
		finally
		{
			completion.TrySetResult();
		}

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handling);
		Assert.Same(failure, exception);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task HandleAsync_PredicateFailureOrCancellation_DoesNotDispatch(bool typed, bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var predicate = new Predicate();
		var handler = new Handler();
		var descriptor = CreateDescriptor(typed, predicate, handler);
		var handling = Invoke(descriptor, typed, cancellation: cancellation.Token).AsTask();

		if(cancel)
		{
			cancellation.Cancel();
			predicate.Completion.SetCanceled(cancellation.Token);
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handling);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
		}
		else
		{
			var failure = new InvalidOperationException("predicate failed");
			predicate.Completion.SetException(failure);
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => handling);
			Assert.Same(failure, exception);
		}

		Assert.Equal(0, handler.CallCount);
	}

	private static EventDescriptor CreateDescriptor(bool typed, Predicate predicate, params Handler[] handlers)
	{
		EventDescriptor descriptor = new EventDescriptor("Changed", [typeof(string)]);
		descriptor.Predication = predicate;
		foreach(var handler in handlers)
			descriptor.Handlers.Add(handler);
		return descriptor;
	}

	private static ValueTask Invoke(EventDescriptor descriptor, bool typed, Parameters parameters = null, CancellationToken cancellation = default) =>
		typed ? descriptor.HandleAsync("argument", parameters, cancellation) :
			descriptor.HandleAsync(typeof(string), "argument", parameters, cancellation);

	private sealed class Predicate : IPredication
	{
		public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public object Argument { get; private set; }
		public Parameters Parameters { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		public ValueTask<bool> PredicateAsync(object argument, CancellationToken cancellation = default) => this.PredicateAsync(argument, null, cancellation);
		public ValueTask<bool> PredicateAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Argument = argument;
			this.Parameters = parameters;
			this.Cancellation = cancellation;
			return new(this.Completion.Task);
		}
	}

	private sealed class Handler(Func<ValueTask> behavior = null) : IHandler<string>
	{
		public int CallCount { get; private set; }
		public object Argument { get; private set; }
		public Parameters Parameters { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public bool Typed { get; private set; }

		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => this.Handle(argument, parameters, cancellation, false);
		public ValueTask HandleAsync(string argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(string argument, Parameters parameters, CancellationToken cancellation = default) => this.Handle(argument, parameters, cancellation, true);

		private ValueTask Handle(object argument, Parameters parameters, CancellationToken cancellation, bool typed)
		{
			this.CallCount++;
			this.Argument = argument;
			this.Parameters = parameters;
			this.Cancellation = cancellation;
			this.Typed = typed;
			return behavior?.Invoke() ?? ValueTask.CompletedTask;
		}
	}
}

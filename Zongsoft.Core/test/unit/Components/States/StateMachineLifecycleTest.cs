using System;
using System.Reflection;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Data.Transactions;

namespace Zongsoft.Components.States.Tests;

public class StateMachineLifecycleTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_PendingHook_SharesCompletionOrFailure(bool fail)
	{
		var release = NewGate();
		var failure = new InvalidOperationException();
		var machine = new DisposalProbe(release.Task);
		var state = new StateProbe(new DiagramProbe(), 1);

		try
		{
			var disposal = machine.DisposeAsync().AsTask();
			Assert.False(disposal.IsCompleted);
			Assert.Same(disposal, machine.DisposeAsync().AsTask());
			Assert.Equal(1, machine.Disposals);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => machine.RunAsync(state, "closed").AsTask());

			if(fail)
				release.SetException(failure);
			else
				release.SetResult();

			if(fail)
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(_timeout)));
			else
				await disposal.WaitAsync(_timeout);

			Assert.Same(disposal, machine.DisposeAsync().AsTask());
			Assert.Equal(1, machine.Disposals);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => machine.RunAsync(state, "closed").AsTask());
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => machine.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_AfterRun_WaitsForTransactionCommit(bool fail)
	{
		var ambient = Transaction.Current;
		var enlistment = new EnlistmentProbe();
		var handler = new HandlerProbe(enlistment);
		var machine = new StateMachine(new ServiceProviderProbe(handler));
		var failure = new InvalidOperationException();
		await machine.RunAsync(new StateProbe(new DiagramProbe(), 1), "transition");
		Assert.Equal(1, handler.Handles);
		Assert.Equal(0, handler.Finishes);

		try
		{
			var disposal = machine.DisposeAsync().AsTask();
			await enlistment.Entered.Task.WaitAsync(_timeout);
			Assert.Same(ambient, Transaction.Current);
			Assert.False(disposal.IsCompleted);
			Assert.Same(disposal, machine.DisposeAsync().AsTask());
			Assert.Equal(1, handler.Finishes);
			Assert.Equal(EnlistmentPhase.Commit, enlistment.Phase);
			Assert.False(enlistment.Cancellation.CanBeCanceled);

			if(fail)
				enlistment.Release.SetException(failure);
			else
				enlistment.Release.SetResult();

			if(fail)
			{
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => disposal.WaitAsync(_timeout)));
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => machine.DisposeAsync().AsTask()));
			}
			else
				await disposal.WaitAsync(_timeout);

			Assert.Equal(1, enlistment.Calls);
			Assert.Equal(fail ? TransactionStatus.Undetermined : TransactionStatus.Committed, handler.Transaction.Context.Status);
			Assert.Same(ambient, Transaction.Current);
		}
		finally
		{
			enlistment.Release.TrySetResult();
			await Record.ExceptionAsync(() => machine.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	[Fact]
	public async Task DisposeAsync_FinishAndRollbackFail_PreservesBothErrors()
	{
		var operationFailure = new InvalidOperationException();
		var cleanupFailure = new ApplicationException();
		var enlistment = new EnlistmentProbe();
		var handler = new HandlerProbe(enlistment) { Failure = operationFailure };
		var machine = new StateMachine(new ServiceProviderProbe(handler));
		await machine.RunAsync(new StateProbe(new DiagramProbe(), 1), "transition");

		try
		{
			var disposal = machine.DisposeAsync().AsTask();
			await enlistment.Entered.Task.WaitAsync(_timeout);
			Assert.False(disposal.IsCompleted);
			Assert.Equal(EnlistmentPhase.Rollback, enlistment.Phase);
			enlistment.Release.SetException(cleanupFailure);

			var failure = await Assert.ThrowsAsync<AggregateException>(() => disposal.WaitAsync(_timeout));
			Assert.Collection(failure.InnerExceptions,
				error => Assert.Same(operationFailure, Assert.IsType<TargetInvocationException>(error).InnerException),
				error => Assert.Same(cleanupFailure, error));
			Assert.Same(failure, await Assert.ThrowsAsync<AggregateException>(() => machine.DisposeAsync().AsTask()));
			Assert.Equal(1, handler.Finishes);
			Assert.Equal(1, enlistment.Calls);
		}
		finally
		{
			enlistment.Release.TrySetResult();
			await Record.ExceptionAsync(() => machine.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}

	private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	[Fact]
	public async Task RunAsync_AwaitsDiagramAndHandlerBeforeReturning()
	{
		using var cancellation = new CancellationTokenSource();
		var stages = new Steps("get", "transferring", "handle", "transferred");
		var diagram = new AsyncDiagram(stages);
		var handler = new AsyncHandler(stages);
		var machine = new StateMachine(new ServiceProviderProbe(handler));
		var operation = machine.RunAsync(new StateProbe(diagram, 1), "transition", new Dictionary<object, object> { ["item"] = 17 }, cancellation.Token).AsTask();

		try
		{
			await stages.AdvanceAsync(operation, cancellation.Token);
			await operation.WaitAsync(_timeout);
			Assert.Equal(42, handler.Context.Key);
			Assert.Equal(0, handler.Context.State.Source);
			Assert.Equal(1, handler.Context.State.Destination);
			Assert.Equal("transition", handler.Context.Description);
			Assert.Equal(17, handler.Context.Parameters["item"]);
			Assert.Equal(0, diagram.Writes);
		}
		finally
		{
			stages.ReleaseAll();
			await machine.DisposeAsync();
		}
	}

	[Fact]
	public async Task DisposeAsync_AwaitsFinishAndStateWritesInOriginalFrameOrder()
	{
		var stages = new Steps("finish:1", "set:1", "finish:2", "set:2");
		var enlistment = new EnlistmentProbe();
		var diagram = new AsyncDiagram(stages);
		var handler = new AsyncHandler(stages, enlistment);
		var machine = new StateMachine(new ServiceProviderProbe(handler));
		await machine.RunAsync(new StateProbe(diagram, 1), "first");
		await machine.RunAsync(new StateProbe(diagram, 2), "second");
		var operation = machine.DisposeAsync().AsTask();

		try
		{
			Assert.Same(operation, machine.DisposeAsync().AsTask());
			await stages.AdvanceAsync(operation, CancellationToken.None);
			await enlistment.Entered.Task.WaitAsync(_timeout);
			Assert.False(operation.IsCompleted);
			Assert.Equal(2, diagram.Writes);
			Assert.Equal([1, 2], diagram.WrittenStates);
			Assert.Equal(["first", "second"], diagram.Descriptions);
			Assert.Equal(EnlistmentPhase.Commit, enlistment.Phase);
			Assert.Equal(CancellationToken.None, enlistment.Cancellation);
			enlistment.Release.TrySetResult();
			await operation.WaitAsync(_timeout);
			Assert.Equal(1, enlistment.Calls);
		}
		finally
		{
			stages.ReleaseAll();
			enlistment.Release.TrySetResult();
			await machine.DisposeAsync();
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RunAsync_CancelledStateReadDoesNotTransferOrFinish(bool beforeStart)
	{
		using var cancellation = new CancellationTokenSource();
		var stages = new Steps("get");
		var diagram = new AsyncDiagram(stages);
		var handler = new AsyncHandler(stages);
		await using var machine = new StateMachine(new ServiceProviderProbe(handler));

		if(beforeStart)
			cancellation.Cancel();

		var operation = machine.RunAsync(new StateProbe(diagram, 1), "cancelled", cancellation.Token).AsTask();

		try
		{
			if(!beforeStart)
			{
				await stages.Entered[0].Task.WaitAsync(_timeout);
				Assert.False(operation.IsCompleted);
				cancellation.Cancel();
			}

			var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(_timeout));
			Assert.Equal(cancellation.Token, error.CancellationToken);
			Assert.Null(handler.Context);
			Assert.Equal(beforeStart ? 0 : 1, diagram.Reads);
			await machine.DisposeAsync();
			Assert.Equal(0, diagram.Writes);
		}
		finally { stages.ReleaseAll(); }
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeAsync_PendingFinishFailure_PreservesReflectionContract(bool duringWrite)
	{
		var point = duringWrite ? "set:1" : "finish:1";
		var stages = new Steps(point);
		var diagram = new AsyncDiagram(stages);
		var handler = new AsyncHandler(stages);
		var machine = new StateMachine(new ServiceProviderProbe(handler));
		var failure = new InvalidOperationException();
		await machine.RunAsync(new StateProbe(diagram, 1), "failure");
		var operation = machine.DisposeAsync().AsTask();

		try
		{
			await stages.Entered[0].Task.WaitAsync(_timeout);
			Assert.False(operation.IsCompleted);
			stages.Fail(point, failure);
			var error = await Assert.ThrowsAsync<TargetInvocationException>(() => operation.WaitAsync(_timeout));
			Assert.Same(failure, error.InnerException);
			Assert.Same(error, await Assert.ThrowsAsync<TargetInvocationException>(() => machine.DisposeAsync().AsTask()));
			Assert.Equal(0, diagram.Writes);
		}
		finally
		{
			stages.ReleaseAll();
			await Record.ExceptionAsync(() => machine.DisposeAsync().AsTask().WaitAsync(_timeout));
		}
	}
	private sealed class Steps(params string[] names)
	{
		private readonly TaskCompletionSource[] _released = names.Select(_ => NewGate()).ToArray();
		private readonly ConcurrentQueue<(string Name, CancellationToken Token)> _calls = new();
		public TaskCompletionSource[] Entered { get; } = names.Select(_ => NewGate()).ToArray();
		public async ValueTask WaitAsync(string name, CancellationToken cancellation)
		{
			var index = Array.IndexOf(names, name);
			if(index < 0)
				return;

			_calls.Enqueue((name, cancellation));
			this.Entered[index].TrySetResult();
			await _released[index].Task.WaitAsync(cancellation);
		}
		public async Task AdvanceAsync(Task operation, CancellationToken cancellation)
		{
			for(var index = 0; index < names.Length; index++)
			{
				await this.Entered[index].Task.WaitAsync(_timeout);
				Assert.False(operation.IsCompleted);
				Assert.Equal(names.Take(index + 1), _calls.Select(call => call.Name));
				Assert.All(_calls, call => Assert.Equal(cancellation, call.Token));
				_released[index].TrySetResult();
			}
		}
		public void ReleaseAll()
		{
			foreach(var release in _released)
				release.TrySetResult();
		}
		public void Fail(string name, Exception failure) => _released[Array.IndexOf(names, name)].TrySetException(failure);
	}

	private sealed class AsyncDiagram(Steps steps) : StateDiagramBase<int, int>(new ServiceProviderProbe(null))
	{
		public int Reads { get; private set; }
		public int Writes { get; private set; }
		public List<int> WrittenStates { get; } = [];
		public List<string> Descriptions { get; } = [];
		protected override bool CanTransfer(int source, int destination) => destination == source + 1;
		protected override async ValueTask<State<int, int>> GetStateAsync(int key, CancellationToken cancellation)
		{
			this.Reads++;
			await steps.WaitAsync("get", cancellation);
			return new StateProbe(this, 0);
		}
		protected override ValueTask OnTransferingAsync(IStateContext<int, int> context, CancellationToken cancellation) => steps.WaitAsync("transferring", cancellation);
		protected override ValueTask OnTransferredAsync(IStateContext<int, int> context, CancellationToken cancellation) => steps.WaitAsync("transferred", cancellation);
		protected override async ValueTask<bool> SetStateAsync(int key, int value, string description, IDictionary<object, object> parameters, CancellationToken cancellation)
		{
			Assert.Equal(42, key);
			await steps.WaitAsync($"set:{value}", cancellation);
			this.Writes++;
			this.WrittenStates.Add(value);
			this.Descriptions.Add(description);
			return true;
		}
	}

	private sealed class AsyncHandler(Steps steps, IEnlistment enlistment = null) : StateHandlerBase<int, int>(new ServiceProviderProbe(null))
	{
		private bool _enlisted;
		public StateContext<int, int> Context { get; private set; }
		protected override async ValueTask OnHandleAsync(StateContext<int, int> context, CancellationToken cancellation)
		{
			this.Context = context;
			await steps.WaitAsync("handle", cancellation);
		}
		protected override async ValueTask OnFinishAsync(StateContext<int, int> context, CancellationToken cancellation)
		{
			Assert.Equal(CancellationToken.None, cancellation);
			if(enlistment != null && !_enlisted)
				_enlisted = Transaction.Current.Context.Enlist(enlistment);

			await steps.WaitAsync($"finish:{context.State.Destination}", cancellation);
			await base.OnFinishAsync(context, cancellation);
		}
	}

	private sealed class DisposalProbe(Task completion) : StateMachine(new ServiceProviderProbe(null))
	{
		public int Disposals { get; private set; }
		protected override async ValueTask DisposeAsyncCore()
		{
			this.Disposals++;
			await completion;
			await base.DisposeAsyncCore();
		}
	}

	private sealed class ServiceProviderProbe(IStateHandler<int, int> handler) : IServiceProvider
	{
		public object GetService(Type serviceType) => serviceType == typeof(IEnumerable<IStateHandler<int, int>>) ? new[] { handler } : null;
	}

	private sealed class HandlerProbe(IEnlistment enlistment) : IStateHandler<int, int>
	{
		public int Handles { get; private set; }
		public int Finishes { get; private set; }
		public Transaction Transaction { get; private set; }
		public Exception Failure { get; init; }
		public ValueTask HandleAsync(IStateContext<int, int> context, CancellationToken cancellation = default) { this.Handles++; return ValueTask.CompletedTask; }
		public ValueTask FinishAsync(IStateContext<int, int> context, CancellationToken cancellation = default)
		{
			this.Finishes++;
			this.Transaction = Assert.IsType<Transaction>(Transaction.Current);
			Assert.True(this.Transaction.Context.Enlist(enlistment));

			if(this.Failure != null)
				throw this.Failure;

			return ValueTask.CompletedTask;
		}
	}

	private sealed class EnlistmentProbe : IEnlistment
	{
		public TaskCompletionSource Entered { get; } = NewGate();
		public TaskCompletionSource Release { get; } = NewGate();
		public int Calls { get; private set; }
		public EnlistmentPhase Phase { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public async ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Phase = context.Phase;
			this.Cancellation = cancellation;
			this.Entered.TrySetResult();
			await this.Release.Task;
		}
	}

	private sealed class StateProbe(IStateDiagram<int, int> diagram, int value) : State<int, int>(diagram, 42, value);

	private sealed class DiagramProbe : IStateDiagram<int, int>
	{
		public StateVector<int>[] Vectors => [new(0, 1)];
		public bool CanTransfer(int source, int destination) => source == 0 && destination == 1;
		public ValueTask TransferAsync(IStateContext<int, int> context, IStateHandler<int, int> handler, CancellationToken cancellation = default) => handler.HandleAsync(context, cancellation);
		public ValueTask<State<int, int>> GetStateAsync(int key, CancellationToken cancellation = default) => ValueTask.FromResult<State<int, int>>(new StateProbe(this, 0));
		public ValueTask<bool> SetStateAsync(int key, int value, string description, IDictionary<object, object> parameters, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<bool> SetStateAsync(State<int, int> state, string description, IDictionary<object, object> parameters, CancellationToken cancellation = default) => throw new NotSupportedException();
	}
}

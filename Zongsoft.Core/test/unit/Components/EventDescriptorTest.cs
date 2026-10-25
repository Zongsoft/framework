using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class EventDescriptorTest
{
	private MyEventTrigger _trigger = new();

	[Fact]
	public void Equality_IgnoresCaseAndUsesCompatibleHashCode()
	{
		var first = new EventDescriptor("Target.Changed");
		var second = new EventDescriptor("target.changed");
		var descriptors = new Dictionary<EventDescriptor, string> { [first] = "registered" };

		Assert.True(first.Equals(second));
		Assert.True(first.Equals((object)second));
		Assert.False(first.Equals(null));
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
		Assert.Equal("registered", descriptors[second]);
	}

	[Fact]
	public async Task TestEventHandler1()
	{
		var name = nameof(_trigger.MyEvent);
		var descriptor = new EventDescriptor(name, [typeof(EventArgs)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.MyEvent));
		descriptor.Handlers.Add(new MyEventHandler1());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnMyEvent().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.MyEvent));
		Assert.False(_trigger.HasMyEvent);
	}

	[Fact]
	public async Task TestEventHandler2()
	{
		var name = nameof(_trigger.MyEventWithArgs);
		var descriptor = new EventDescriptor(name, [typeof(MyEventArgs)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.MyEventWithArgs));
		descriptor.Handlers.Add(new MyEventHandler2());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnMyEventWithArgs().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.MyEventWithArgs));
		Assert.False(_trigger.HasMyEventWithArgs);
	}

	[Fact]
	public async Task TestAction()
	{
		var name = nameof(_trigger.Action0);
		var descriptor = new EventDescriptor(name);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action0));
		Assert.NotNull(_trigger.Action0);

		descriptor.Handlers.Add(new MyHandlerObject());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction0().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action0));
		Assert.Null(_trigger.Action0);
	}

	[Fact]
	public async Task TestAction1_Object()
	{
		var name = nameof(_trigger.Action1_Object);
		var descriptor = new EventDescriptor(name, [typeof(object)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action1_Object));
		Assert.NotNull(_trigger.Action1_Object);

		descriptor.Handlers.Add(new MyHandlerObject());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction1_Object().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action1_Object));
		Assert.Null(_trigger.Action1_Object);
	}

	[Fact]
	public async Task TestAction1_Generic()
	{
		var name = nameof(_trigger.Action1_Generic);
		var descriptor = new EventDescriptor(name, [typeof(MyArgument)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action1_Generic));
		Assert.NotNull(_trigger.Action1_Generic);

		descriptor.Handlers.Add(new MyHandlerGeneric());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction1_Generic().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action1_Generic));
		Assert.Null(_trigger.Action1_Generic);
	}

	[Fact]
	public async Task TestAction2_Object()
	{
		var name = nameof(_trigger.Action2_Object);
		var descriptor = new EventDescriptor(name, [typeof(object)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action2_Object));
		Assert.NotNull(_trigger.Action2_Object);

		descriptor.Handlers.Add(new MyHandlerObject());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction2_Object().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action2_Object));
		Assert.Null(_trigger.Action2_Object);
	}

	[Fact]
	public async Task TestAction2_Generic()
	{
		var name = nameof(_trigger.Action2_Generic);
		var descriptor = new EventDescriptor(name, [typeof(MyArgument)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action2_Generic));
		Assert.NotNull(_trigger.Action2_Generic);

		descriptor.Handlers.Add(new MyHandlerGeneric());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction2_Generic().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action2_Generic));
		Assert.Null(_trigger.Action2_Generic);
	}

	[Fact]
	public async Task TestAction3_Object()
	{
		var name = nameof(_trigger.Action3_Object);
		var descriptor = new EventDescriptor(name, [typeof(object)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action3_Object));
		Assert.NotNull(_trigger.Action3_Object);

		descriptor.Handlers.Add(new MyHandlerObject());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction3_Object().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action3_Object));
		Assert.Null(_trigger.Action3_Object);
	}

	[Fact]
	public async Task TestAction3_Generic()
	{
		var name = nameof(_trigger.Action3_Generic);
		var descriptor = new EventDescriptor(name, [typeof(MyArgument)]);
		Assert.Equal(name, descriptor.Name);
		Assert.Null(descriptor.Title);
		Assert.Null(descriptor.Description);
		Assert.Empty(descriptor.Handlers);

		//Bind
		descriptor.Bind(_trigger, nameof(_trigger.Action3_Generic));
		Assert.NotNull(_trigger.Action3_Generic);

		descriptor.Handlers.Add(new MyHandlerGeneric());
		Assert.Single(descriptor.Handlers);
		await Assert.ThrowsAsync<MyHandlerException>(() => _trigger.OnAction3_Generic().AsTask());

		//Unbind
		descriptor.Unbind(_trigger, nameof(_trigger.Action3_Generic));
		Assert.Null(_trigger.Action3_Generic);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(0, 1)]
	[InlineData(0, 2)]
	[InlineData(1, 0)]
	[InlineData(1, 1)]
	[InlineData(1, 2)]
	[InlineData(2, 0)]
	[InlineData(2, 1)]
	[InlineData(2, 2)]
	public async Task Bind_AwaitableMembers_WaitsForHandlerAndPreservesOutcome(int member, int outcome)
	{
		using var cancellation = new CancellationTokenSource();
		var trigger = new AsyncTrigger();
		var handler = new PendingHandler();
		var descriptor = new EventDescriptor($"Changed-{Guid.NewGuid():N}", [typeof(string)]);
		var parameters = Collections.Parameters.Parameter("sequence", 19);
		var name = member switch
		{
			0 => nameof(AsyncTrigger.TaskField),
			1 => nameof(AsyncTrigger.ValueTaskProperty),
			_ => nameof(AsyncTrigger.Changed),
		};
		descriptor.Handlers.Add(handler);
		descriptor.Bind(trigger, name);
		var operation = trigger.RaiseAsync(member, "payload", parameters, cancellation.Token);
		var failure = new InvalidOperationException("bound handler failed");

		try
		{
			Assert.False(operation.IsCompleted);
			Assert.Equal(1, handler.CallCount);
			Assert.Equal("payload", handler.Argument);
			Assert.Same(parameters, handler.Parameters);
			Assert.Equal(cancellation.Token, handler.Cancellation);

			if(outcome == 1)
			{
				handler.Completion.SetException(failure);
				var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);
				Assert.Same(failure, exception);
			}
			else if(outcome == 2)
			{
				cancellation.Cancel();
				handler.Completion.SetCanceled(cancellation.Token);
				var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
				Assert.Equal(cancellation.Token, exception.CancellationToken);
			}
			else
			{
				handler.Completion.SetResult();
				await operation.WaitAsync(TimeSpan.FromSeconds(5));
			}
		}
		finally
		{
			handler.Completion.TrySetResult();
			descriptor.Unbind(trigger, name);
		}

		await trigger.RaiseAsync(member, "after-unbind", parameters, CancellationToken.None);
		Assert.Equal(1, handler.CallCount);
	}

	[Fact]
	public void Bind_VoidDelegate_RejectsUnobservableAsynchronousDispatch()
	{
		var trigger = new AsyncTrigger();
		var descriptor = new EventDescriptor("Synchronous");

		Assert.Throws<NotSupportedException>(() => descriptor.Bind(trigger, nameof(AsyncTrigger.Synchronous)));
		Assert.Null(trigger.Synchronous);
	}

	private sealed class PendingHandler : HandlerBase<string>
	{
		public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int CallCount { get; private set; }
		public string Argument { get; private set; }
		public Collections.Parameters Parameters { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		protected override ValueTask OnHandleAsync(string argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			this.CallCount++;
			this.Argument = argument;
			this.Parameters = parameters;
			this.Cancellation = cancellation;
			return new(this.Completion.Task);
		}
	}

	private sealed class AsyncTrigger
	{
		public Func<object, string, Collections.Parameters, CancellationToken, Task> TaskField = null;
		public Func<object, string, Collections.Parameters, CancellationToken, ValueTask> ValueTaskProperty { get; set; }
		public event Func<object, string, Collections.Parameters, CancellationToken, ValueTask> Changed;
		public Action Synchronous { get; set; }

		public async Task RaiseAsync(int member, string argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			if(member == 0)
				await (this.TaskField?.Invoke(this, argument, parameters, cancellation) ?? Task.CompletedTask);
			else if(member == 1)
				await (this.ValueTaskProperty?.Invoke(this, argument, parameters, cancellation) ?? ValueTask.CompletedTask);
			else if(this.Changed != null)
			{
				foreach(Func<object, string, Collections.Parameters, CancellationToken, ValueTask> handler in this.Changed.GetInvocationList())
					await handler(this, argument, parameters, cancellation);
			}
		}
	}

	public class MyArgument
	{
		public MyArgument() { }
		public MyArgument(string name, object value)
		{
			this.Name = name;
			this.Value = value;
		}

		public string Name { get; set; }
		public object Value { get; set; }
	}

	public class MyEventArgs : EventArgs
	{
		public MyEventArgs(string name, object value)
		{
			this.Name = name;
			this.Value = value;
		}

		public string Name { get; set; }
		public object Value { get; set; }
	}

	public class MyEventTrigger
	{
		public event Func<object, EventArgs, ValueTask> MyEvent;
		public event Func<object, MyEventArgs, ValueTask> MyEventWithArgs;

		public bool HasMyEvent => MyEvent != null;
		public bool HasMyEventWithArgs => MyEventWithArgs != null;

		public async ValueTask OnMyEvent()
		{
			if(this.MyEvent != null)
			{
				foreach(Func<object, EventArgs, ValueTask> handler in this.MyEvent.GetInvocationList())
					await handler(this, EventArgs.Empty);
			}
		}

		public async ValueTask OnMyEventWithArgs()
		{
			if(this.MyEventWithArgs != null)
			{
				foreach(Func<object, MyEventArgs, ValueTask> handler in this.MyEventWithArgs.GetInvocationList())
					await handler(this, new MyEventArgs("MY_NAME", "MY_VALUE"));
			}
		}

		public Func<ValueTask> Action0 { get; set; }
		public Func<object, ValueTask> Action1_Object { get; set; }
		public Func<MyArgument, ValueTask> Action1_Generic { get; set; }
		public Func<object, object, ValueTask> Action2_Object { get; set; }
		public Func<object, MyArgument, ValueTask> Action2_Generic { get; set; }
		public Func<object, object, Collections.Parameters, ValueTask> Action3_Object { get; set; }
		public Func<object, MyArgument, Collections.Parameters, ValueTask> Action3_Generic { get; set; }

		public ValueTask OnAction0() => this.Action0?.Invoke() ?? ValueTask.CompletedTask;
		public ValueTask OnAction1_Object() => this.Action1_Object?.Invoke("request") ?? ValueTask.CompletedTask;
		public ValueTask OnAction1_Generic() => this.Action1_Generic?.Invoke(new MyArgument()) ?? ValueTask.CompletedTask;
		public ValueTask OnAction2_Object() => this.Action2_Object?.Invoke(this, "request") ?? ValueTask.CompletedTask;
		public ValueTask OnAction2_Generic() => this.Action2_Generic?.Invoke(this, new MyArgument()) ?? ValueTask.CompletedTask;
		public ValueTask OnAction3_Object() => this.Action3_Object?.Invoke(this, "request", Collections.Parameters.Parameter("Unnamed")) ?? ValueTask.CompletedTask;
		public ValueTask OnAction3_Generic() => this.Action3_Generic?.Invoke(this, new MyArgument(), Collections.Parameters.Parameter("Unnamed")) ?? ValueTask.CompletedTask;
	}

	public class MyEventHandler1 : HandlerBase<EventArgs>
	{
		protected override ValueTask OnHandleAsync(EventArgs argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			throw new MyHandlerException(argument, parameters);
		}
	}

	public class MyEventHandler2 : HandlerBase<MyEventArgs>
	{
		protected override ValueTask OnHandleAsync(MyEventArgs argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			throw new MyHandlerException(argument, parameters);
		}
	}

	public class MyHandlerObject : HandlerBase<object>
	{
		protected override ValueTask OnHandleAsync(object argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			throw new MyHandlerException(argument, parameters);
		}
	}

	public class MyHandlerGeneric : HandlerBase<MyArgument>
	{
		protected override ValueTask OnHandleAsync(MyArgument argument, Collections.Parameters parameters, CancellationToken cancellation)
		{
			throw new MyHandlerException(argument, parameters);
		}
	}

	public class MyHandlerException : ApplicationException
	{
		public MyHandlerException(object argument, Collections.Parameters parameters = null) : base($"The handler error.")
		{
			this.Argument = argument;
			this.Parameters = parameters;
		}

		public object Argument { get; }
		public Collections.Parameters Parameters { get; }
	}
}

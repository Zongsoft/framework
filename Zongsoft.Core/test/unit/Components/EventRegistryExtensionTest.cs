using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[CollectionDefinition(nameof(EventRegistryExtensionCollection), DisableParallelization = true)]
public sealed class EventRegistryExtensionCollection;

[Collection(nameof(EventRegistryExtensionCollection))]
public class EventRegistryExtensionTest
{
	[Fact]
	public void ExtensionsShareDeclarationsAndFiltersWithRoot()
	{
		var root = new RootRegistry();
		var first = new ExtensionRegistry(root, "First.Created");
		var second = new ExtensionRegistry(root, "Second.Created");
		var nested = new ExtensionRegistry(first, "Third.Created");

		Assert.Same(root.Events, first.Events);
		Assert.Same(root.Events, second.Events);
		Assert.Same(root.Events, nested.Events);
		Assert.Same(root.Filters, nested.Filters);
		Assert.Equal(root.Name, nested.Name);
		Assert.Equal(3, root.Events.Count);
		Assert.Same(first.Descriptor, root["First.Created"]);
		Assert.Same(second.Descriptor, root["Second.Created"]);
		Assert.Same(nested.Descriptor, root["Third.Created"]);
		Assert.Equal("Common:Third.Created", nested.Descriptor.QualifiedName);

		Assert.Throws<ArgumentException>(() => new ExtensionRegistry(root, "First.Created"));
		Assert.Equal(3, root.Events.Count);
		Assert.Same(first.Descriptor, root["First.Created"]);
	}

	[Fact]
	public void NullRootIsRejected()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => new ExtensionRegistry(null, "Created"));

		Assert.Equal("registry", exception.ParamName);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task PublicationUsesRootContextAndFiltering(bool remote)
	{
		var root = new RootRegistry();
		var extension = new ExtensionRegistry(new ExtensionRegistry(root, "Other.Created"), "Building.Created");
		var filter = new Filter(root.Steps);
		root.Filters.Add(filter);
		using var cancellation = new CancellationTokenSource();
		var parameters = Parameters.Parameter("source", "test");
		extension.Descriptor.Handlers.Add(new Handler((argument, received, token) =>
		{
			Assert.Equal("created", argument);
			Assert.Same(parameters, received);
			Assert.Equal(cancellation.Token, token);
			root.Steps.Add("Handler");
			return ValueTask.CompletedTask;
		}));

		if(remote)
		{
			var exchanger = EventExchanger.Instance;
			var locator = exchanger.Locator;

			try
			{
				exchanger.Locator = name => name == extension.Descriptor.QualifiedName ? (root, extension.Descriptor) : (null, null);
				await ((IEventReceiver)exchanger).ReceiveAsync(extension.Descriptor.QualifiedName, "created", parameters, cancellation.Token);
			}
			finally
			{
				exchanger.Locator = locator;
			}
		}
		else
			Assert.True(await extension.PublishAsync("created", parameters, cancellation.Token));

		Assert.Same(root, root.Context.Registry);
		Assert.Same(root.Context, filter.Context);
		Assert.Equal("Common:Building.Created", root.Context.QualifiedName);
		Assert.Equal(new[] { "Context", "Before", "FilterBefore", "Handler", "After", "FilterAfter" }, root.Steps);
	}

	[Fact]
	public async Task RootRegistrationAndExtensionUnregistrationUseSameHandlers()
	{
		var root = new RootRegistry();
		var extension = new ExtensionRegistry(root, "Building.Created");
		var count = 0;
		var handler = new Handler((_, _, _) =>
		{
			count++;
			return ValueTask.CompletedTask;
		});

		Assert.True(root.Register(extension.Descriptor.Name, handler));
		Assert.True(await extension.PublishAsync("first"));
		Assert.Equal(1, count);

		Assert.True(extension.Unregister(extension.Descriptor.Name, handler));
		Assert.True(await extension.PublishAsync("second"));
		Assert.Equal(1, count);
		Assert.False(root.Register("Unknown", handler));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task ExtensionAwaitsHandlerAndPropagatesFailureOrCancellation(bool cancel)
	{
		var extension = new ExtensionRegistry(new RootRegistry(), "Building.Created");
		using var cancellation = new CancellationTokenSource();
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		extension.Descriptor.Handlers.Add(new Handler((_, _, token) => new(completion.Task.WaitAsync(token))));
		var pending = extension.PublishAsync("created", null, cancellation.Token).AsTask();

		Assert.False(pending.IsCompleted);

		try
		{
			if(cancel)
			{
				cancellation.Cancel();
				var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
				Assert.Equal(cancellation.Token, exception.CancellationToken);
			}
			else
			{
				var failure = new InvalidOperationException();
				completion.SetException(failure);
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending));
			}
		}
		finally
		{
			completion.TrySetResult();
		}
	}

	private sealed class RootRegistry() : EventRegistryBase("Common")
	{
		public List<string> Steps { get; } = [];
		public EventContext Context { get; private set; }
		protected override EventContext<TArgument> GetContext<TArgument>(string name, TArgument argument, Parameters parameters)
		{
			this.Steps.Add("Context");
			var context = base.GetContext(name, argument, parameters);
			this.Context = context;
			return context;
		}
		protected override ValueTask OnFiltering(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation)
		{
			this.Steps.Add("Before");
			return base.OnFiltering(filter, context, cancellation);
		}
		protected override ValueTask OnFiltered(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation)
		{
			this.Steps.Add("After");
			return base.OnFiltered(filter, context, cancellation);
		}
	}

	private sealed class ExtensionRegistry : EventRegistryBase
	{
		public ExtensionRegistry(EventRegistryBase registry, string name) : base(registry)
		{
			this.Descriptor = new(name);
			this.Event(this.Descriptor);
		}
		public EventDescriptor<string> Descriptor { get; }
		public ValueTask<bool> PublishAsync(string argument, Parameters parameters = null, CancellationToken cancellation = default) => this.RaiseAsync(this.Descriptor.Name, argument, parameters, cancellation);
		protected override EventContext<TArgument> GetContext<TArgument>(string name, TArgument argument, Parameters parameters) => throw new InvalidOperationException("The root must create the context.");
		protected override ValueTask OnFiltering(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation) => throw new InvalidOperationException("The root must execute filters.");
		protected override ValueTask OnFiltered(IFilter<EventContext> filter, EventContext context, CancellationToken cancellation) => throw new InvalidOperationException("The root must execute filters.");
	}

	private sealed class Handler(Func<string, Parameters, CancellationToken, ValueTask> handle) : HandlerBase<string>
	{
		protected override ValueTask OnHandleAsync(string argument, Parameters parameters, CancellationToken cancellation) => handle(argument, parameters, cancellation);
	}

	private sealed class Filter(List<string> steps) : IFilter<EventContext>
	{
		public EventContext Context { get; private set; }
		public ValueTask OnFiltering(EventContext context, CancellationToken cancellation)
		{
			this.Context = context;
			steps.Add("FilterBefore");
			return ValueTask.CompletedTask;
		}
		public ValueTask OnFiltered(EventContext context, CancellationToken cancellation)
		{
			Assert.Same(this.Context, context);
			steps.Add("FilterAfter");
			return ValueTask.CompletedTask;
		}
	}
}

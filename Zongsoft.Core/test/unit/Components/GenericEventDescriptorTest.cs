using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Common;
using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[Collection(nameof(EventRegistryExtensionCollection))]
public class GenericEventDescriptorTest
{
	[Fact]
	public void ConstructorsPreserveMetadataAndDeclareOneContract()
	{
		var predicate = Predication.Predicate<string>(value => value.Length > 0);
		var descriptor = new EventDescriptor<string>(predicate, "Changed", "Title", "Description");
		Assert.Equal("Changed", descriptor.Name);
		Assert.Equal("Title", descriptor.Title);
		Assert.Equal("Description", descriptor.Description);
		Assert.Same(predicate, descriptor.Predication);
		Assert.Equal(typeof(string), Assert.Single(descriptor.ArgumentTypes));
		Assert.False(descriptor.Supports(typeof(object)));
		Assert.Throws<NotSupportedException>(() => ((IList<Type>)descriptor.ArgumentTypes)[0] = typeof(int));
		Assert.Null(new EventDescriptor<string>((IPredication<string>)null, "Changed").Predication);
		Assert.Equal(typeof(IEnumerable<string>), Assert.Single(new EventDescriptor<IEnumerable<string>>("Batch").ArgumentTypes));
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task BothDeclarationsAwaitTheSamePredicate(bool generic, bool accepted)
	{
		var calls = 0;
		var parameters = Parameters.Parameter("source", "predicate");
		using var cancellation = new CancellationTokenSource();
		var predicate = Predication.Predicate<string>(async (argument, values, token) =>
		{
			await Task.Yield();
			Assert.Equal("value", argument);
			Assert.Same(parameters, values);
			Assert.Equal(cancellation.Token, token);
			calls++;
			return accepted;
		});
		EventDescriptor descriptor = generic ? new EventDescriptor<string>(predicate, "Changed") :
			new EventDescriptor("Changed", [typeof(string)]) { Predication = predicate };
		var handler = new CaptureHandler<string>();
		descriptor.Handlers.Add(handler);
		await descriptor.HandleAsync("value", parameters, cancellation.Token);
		Assert.Equal(1, calls);
		Assert.Equal(accepted ? 1 : 0, handler.Arguments.Count);

		if(accepted)
		{
			Assert.Equal("value", Assert.Single(handler.Arguments));
			Assert.Same(parameters, handler.Parameters);
			Assert.Equal(cancellation.Token, handler.Cancellation);
		}
	}

	[Fact]
	public async Task TypedHandleKeepsCollectionContractForArraysAndNull()
	{
		var descriptor = new EventDescriptor<IEnumerable<string>>("Changed");
		var handler = new CaptureHandler<IEnumerable<string>>();
		descriptor.Handlers.Add(handler);
		var values = new[] { "one", "two" };
		using var cancellation = new CancellationTokenSource();
		await descriptor.HandleAsync(values, cancellation.Token);
		Assert.Same(values, Assert.Single(handler.Arguments));
		Assert.Equal(cancellation.Token, handler.Cancellation);

		var parameters = Parameters.Parameter("source", "typed");
		await descriptor.HandleAsync(null, parameters, cancellation.Token);
		Assert.Equal(2, handler.Arguments.Count);
		Assert.Null(handler.Arguments[1]);
		Assert.Same(parameters, handler.Parameters);
		Assert.Throws<ArgumentException>(() => descriptor.Handlers.Add(new CaptureHandler<string>()));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TypedHandlePropagatesPredicateFailureAndCancellation(bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var failure = new InvalidOperationException();
		var descriptor = new EventDescriptor<string>(Predication.Predicate<string>((_, token) =>
			cancel ? ValueTask.FromCanceled<bool>(token) : ValueTask.FromException<bool>(failure)), "Changed");
		var handler = new CaptureHandler<string>();
		descriptor.Handlers.Add(handler);

		if(cancel)
		{
			cancellation.Cancel();
			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => descriptor.HandleAsync("value", cancellation.Token).AsTask());
			Assert.Equal(cancellation.Token, exception.CancellationToken);
		}
		else
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => descriptor.HandleAsync("value").AsTask()));

		Assert.Empty(handler.Arguments);
	}

	[Theory]
	[InlineData(false, false, false)]
	[InlineData(false, false, true)]
	[InlineData(false, true, false)]
	[InlineData(false, true, true)]
	[InlineData(true, false, false)]
	[InlineData(true, false, true)]
	[InlineData(true, true, false)]
	[InlineData(true, true, true)]
	public Task DeclarationFormsInteroperateLocallyAndRemotely(bool genericSource, bool multiple, bool empty) => multiple ?
		RoundTripAsync<IEnumerable<string>>(genericSource, empty ? null : new[] { "one", "two" }) :
		RoundTripAsync<string>(genericSource, empty ? null : "one");

	private static async Task RoundTripAsync<T>(bool genericSource, T argument) where T : class
	{
		var source = new Registry(genericSource ? new EventDescriptor<T>("Changed") : new EventDescriptor("Changed", [typeof(T)]));
		var target = new Registry(genericSource ? new EventDescriptor("Changed", [typeof(T)]) : new EventDescriptor<T>("Changed"));
		var sourceHandler = new CaptureHandler<T>();
		var targetHandler = new CaptureHandler<T>();
		source["Changed"].Handlers.Add(sourceHandler);
		target["Changed"].Handlers.Add(targetHandler);
		var parameters = Parameters.Parameter("source", "wire");
		using var cancellation = new CancellationTokenSource();
		Assert.True(await source.PublishAsync(argument, parameters, cancellation.Token));
		Assert.Equal(argument, Assert.Single(sourceHandler.Arguments));
		Assert.Same(parameters, sourceHandler.Parameters);
		Assert.Equal(cancellation.Token, sourceHandler.Cancellation);
		var data = Events.Marshaler.Marshal(source.Context);
		Assert.Equal(data, Events.Marshaler.Marshal(new EventContext<T>(target, "Changed", argument, parameters)));
		var (type, restored, values) = Events.Marshaler.Unmarshal(target["Changed"], data);
		Assert.Equal(typeof(T), type);
		var exchanger = EventExchanger.Instance;
		var locator = exchanger.Locator;

		try
		{
			exchanger.Locator = _ => (target, target["Changed"]);
			await ((IEventReceiver)exchanger).ReceiveAsync("Generic:Changed", type, restored, values, cancellation.Token);
			Assert.IsType<EventContext<T>>(target.Context);
			Assert.Same(target["Changed"], target.Context.Descriptor);
			Assert.Same(restored, Assert.Single(targetHandler.Arguments));
			Assert.Equal("wire", targetHandler.Parameters["source"]);
			Assert.Equal(cancellation.Token, targetHandler.Cancellation);

			if(argument is IEnumerable<string> items && restored is IEnumerable<string> received)
				Assert.Equal(items.ToArray(), received.ToArray());
			else
				Assert.Equal(argument, restored);
		}
		finally { exchanger.Locator = locator; }
	}

	private sealed class Registry : EventRegistryBase
	{
		public Registry(EventDescriptor descriptor) : base("Generic") => this.Event(descriptor);
		public EventContext Context { get; private set; }
		public ValueTask<bool> PublishAsync<T>(T argument, Parameters parameters, CancellationToken cancellation) => this.RaiseAsync(this["Changed"], argument, parameters, cancellation);
		protected override EventContext<TArgument> GetContext<TArgument>(string name, TArgument argument, Parameters parameters)
		{
			var context = base.GetContext(name, argument, parameters);
			this.Context = context;
			return context;
		}
	}

	private sealed class CaptureHandler<T> : HandlerBase<T>
	{
		public List<T> Arguments { get; } = [];
		public Parameters Parameters { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		protected override ValueTask OnHandleAsync(T argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Arguments.Add(argument);
			this.Parameters = parameters;
			this.Cancellation = cancellation;
			return ValueTask.CompletedTask;
		}
	}
}

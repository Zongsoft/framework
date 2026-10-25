using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[Collection(nameof(EventRegistryExtensionCollection))]
public class EventSignaturesTest
{
	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task LocalAndRemoteUseTheDeclaredContract(bool multiple, bool empty)
	{
		var registry = new Registry();
		var single = new Handler<string>();
		var batch = new Handler<IEnumerable<string>>();
		var broad = new Handler<object>();
		var combined = new CombinedHandler();
		var filter = new Filter();
		registry.Filters.Add(filter);
		registry.Descriptor.Handlers.Add(single);
		registry.Descriptor.Handlers.Add(batch);
		registry.Descriptor.Handlers.Add(broad);
		registry.Descriptor.Handlers.Add(combined);
		registry.Descriptor.Handlers.Add(combined);
		var parameters = Parameters.Parameter("source", "test");
		using var cancellation = new CancellationTokenSource();

		if(multiple)
			await registry.PublishAsync<IEnumerable<string>>(empty ? null : new[] { "one" }, parameters, cancellation.Token);
		else
			await registry.PublishAsync<string>(empty ? null : "one", parameters, cancellation.Token);

		Assert.Single(registry.Events);
		Assert.Equal(4, registry.Descriptor.Handlers.Count);
		Assert.Equal(multiple ? 0 : 1, single.Count);
		Assert.Equal(multiple ? 1 : 0, batch.Count);
		Assert.Equal(1, broad.Count);
		Assert.Equal(multiple ? "batch" : "single", Assert.Single(combined.Calls));
		Assert.Same(parameters, broad.Parameters);
		Assert.Equal(cancellation.Token, broad.Cancellation);
		Assert.Same(registry.Descriptor, filter.Context.Descriptor);
		var argumentType = multiple ? typeof(IEnumerable<string>) : typeof(string);
		Assert.Equal(argumentType, filter.Context.ArgumentType);
		var (restoredType, argument, restoredParameters) = Events.Marshaler.Unmarshal(registry.Descriptor, Events.Marshaler.Marshal(filter.Context));
		Assert.Equal(argumentType, restoredType);
		Assert.Equal("test", restoredParameters["source"]);

		if(empty)
			Assert.Null(argument);
		else if(multiple)
			Assert.Equal(["one"], Assert.IsAssignableFrom<IEnumerable<string>>(argument));
		else
			Assert.Equal("one", argument);

		var exchanger = EventExchanger.Instance;
		var locator = exchanger.Locator;

		try
		{
			exchanger.Locator = _ => (registry, registry.Descriptor);
			await ((IEventReceiver)exchanger).ReceiveAsync(registry.Descriptor.QualifiedName, restoredType, argument, restoredParameters, cancellation.Token);
			Assert.Equal(multiple ? 0 : 2, single.Count);
			Assert.Equal(multiple ? 2 : 0, batch.Count);
			Assert.Equal(2, broad.Count);
			Assert.Equal([multiple ? "batch" : "single", multiple ? "batch" : "single"], combined.Calls);
			Assert.Equal(argumentType, filter.Context.ArgumentType);
			Assert.Equal(multiple ? typeof(EventContext<IEnumerable<string>>) : typeof(EventContext<string>), filter.Context.GetType());
			Assert.Same(restoredParameters, filter.Context.Parameters);
		}
		finally { exchanger.Locator = locator; }
	}

	[Fact]
	public async Task RuntimeTypesAndConversionsDoNotSelectSignatures()
	{
		var registry = new Registry();
		var handler = new Handler<string>();
		registry.Descriptor.Handlers.Add(handler);
		await Assert.ThrowsAsync<ArgumentException>(() => registry.PublishAsync<object>("one").AsTask());
		await Assert.ThrowsAsync<ArgumentException>(() => registry.PublishAsync(new[] { "one" }).AsTask());
		await Assert.ThrowsAsync<ArgumentException>(() => registry.Descriptor.HandleAsync(typeof(string), 42, null).AsTask());
		Assert.Equal(0, handler.Count);
		Assert.Throws<ArgumentException>(() => registry.Descriptor.Handlers.Add(new Handler<int>()));
		Assert.Throws<ArgumentException>(() => registry.Descriptor.Handlers.Add(new UntypedHandler()));
	}

	[Fact]
	public void DeclarationsAreImmutableAndRejectInvalidTypes()
	{
		Type[] types = [typeof(string), typeof(IEnumerable<string>)];
		var descriptor = new EventDescriptor("Changed", types);
		types[0] = typeof(int);
		Assert.True(descriptor.Supports(typeof(string)));
		Assert.False(descriptor.Supports(typeof(int)));
		Assert.Throws<NotSupportedException>(() => ((IList<Type>)descriptor.ArgumentTypes)[0] = typeof(int));
		Assert.Throws<ArgumentException>(() => new EventDescriptor("Changed", []));
		Assert.Throws<ArgumentException>(() => new EventDescriptor("Changed", [typeof(string), typeof(string)]));
		Assert.Throws<ArgumentException>(() => new EventDescriptor("Changed", [typeof(IEnumerable<>)]));
		Assert.Throws<ArgumentException>(() => new EventDescriptor("Changed", [typeof(void)]));
	}

	[Fact]
	public async Task MostSpecificContractWinsAndAmbiguityFailsAtRegistration()
	{
		var descriptor = new EventDescriptor("Changed", [typeof(Both)]);
		Assert.Throws<ArgumentException>(() => descriptor.Handlers.Add(new AmbiguousHandler()));
		Assert.Empty(descriptor.Handlers);
		var handler = new SpecificHandler();
		descriptor.Handlers.Add(handler);
		await descriptor.HandleAsync(new Both());
		Assert.Equal(1, handler.Count);
		Assert.True(descriptor.Handlers.Remove(handler));
		await descriptor.HandleAsync(new Both());
		Assert.Equal(1, handler.Count);
	}

	[Theory]
	[InlineData("{}")]
	[InlineData("{\"Argument\":null}")]
	[InlineData("{\"Signature\":123,\"Argument\":null}")]
	public void WireSignatureIsRequired(string json)
	{
		var registry = new Registry();
		Assert.ThrowsAny<System.Text.Json.JsonException>(() => Events.Marshaler.Unmarshal(registry.Descriptor, Encoding.UTF8.GetBytes(json)));
	}

	[Fact]
	public void WireSignatureMustBelongToTheDescriptor()
	{
		var registry = new Registry();
		Assert.Throws<ArgumentException>(() => Events.Marshaler.Unmarshal(registry.Descriptor, Encoding.UTF8.GetBytes("{\"Signature\":\"Unregistered.Type@Assembly\",\"Argument\":null}")));
	}

	[Fact]
	public async Task BindingOneDescriptorToSeveralMembersKeepsIndependentBindings()
	{
		var registry = new Registry();
		var trigger = new Trigger();
		var other = new Trigger();
		var handler = new CombinedHandler();
		registry.Descriptor.Handlers.Add(handler);
		registry.Descriptor.Bind(trigger, nameof(Trigger.Single));
		registry.Descriptor.Bind(trigger, nameof(Trigger.Batch));
		registry.Descriptor.Bind(other, nameof(Trigger.Single));

		try
		{
			await trigger.Single("one");
			await trigger.Batch(["one"]);
			registry.Descriptor.Unbind(trigger, nameof(Trigger.Single));
			Assert.Null(trigger.Single);
			await other.Single("two");
			Assert.Equal(["single", "batch", "single"], handler.Calls);
		}
		finally
		{
			registry.Descriptor.Unbind(trigger, nameof(Trigger.Batch));
			registry.Descriptor.Unbind(other, nameof(Trigger.Single));
		}
	}

	[Fact]
	public void DerivedContextPreservesArgumentMetadata()
	{
		var context = new DerivedContext(new Registry(), "value");
		Assert.Equal("value", context.GetArgument(out var type));
		Assert.Equal(typeof(string), type);
	}

	private sealed class Registry : EventRegistryBase
	{
		public Registry() : base("Signatures")
		{
			this.Descriptor = new("Changed", [typeof(string), typeof(IEnumerable<string>)]);
			this.Event(this.Descriptor);
		}
		public EventDescriptor Descriptor { get; }
		public ValueTask<bool> PublishAsync<T>(T argument, Parameters parameters = null, CancellationToken cancellation = default) => this.RaiseAsync(this.Descriptor, argument, parameters, cancellation);
	}
	private sealed class DerivedContext(Registry registry, string value) : EventContext<string>(registry, "Changed", value);
	private sealed class Trigger
	{
		public Func<string, ValueTask> Single { get; set; }
		public Func<IEnumerable<string>, ValueTask> Batch { get; set; }
	}
	private sealed class Filter : IFilter<EventContext>
	{
		public EventContext Context { get; private set; }
		public ValueTask OnFiltering(EventContext context, CancellationToken cancellation) { this.Context = context; return ValueTask.CompletedTask; }
		public ValueTask OnFiltered(EventContext context, CancellationToken cancellation) => ValueTask.CompletedTask;
	}
	private sealed class Handler<T> : HandlerBase<T>
	{
		public int Count { get; private set; }
		public Parameters Parameters { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		protected override ValueTask OnHandleAsync(T argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Count++;
			this.Parameters = parameters;
			this.Cancellation = cancellation;
			return ValueTask.CompletedTask;
		}
	}
	private sealed class CombinedHandler : HandlerBase<object>, IHandler<string>, IHandler<IEnumerable<string>>
	{
		public List<string> Calls { get; } = [];
		public ValueTask HandleAsync(string argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(string argument, Parameters parameters, CancellationToken cancellation = default) { this.Calls.Add("single"); return ValueTask.CompletedTask; }
		public ValueTask HandleAsync(IEnumerable<string> argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(IEnumerable<string> argument, Parameters parameters, CancellationToken cancellation = default) { this.Calls.Add("batch"); return ValueTask.CompletedTask; }
		protected override ValueTask OnHandleAsync(object argument, Parameters parameters, CancellationToken cancellation) => throw new InvalidOperationException("An exact contract must take precedence.");
	}
	private interface IFirst;
	private interface ISecond;
	private sealed class Both : IFirst, ISecond;
	private class AmbiguousHandler : HandlerBase<IFirst>, IHandler<ISecond>
	{
		protected override ValueTask OnHandleAsync(IFirst argument, Parameters parameters, CancellationToken cancellation) => throw new InvalidOperationException();
		public ValueTask HandleAsync(ISecond argument, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask HandleAsync(ISecond argument, Parameters parameters, CancellationToken cancellation = default) => throw new InvalidOperationException();
	}
	private sealed class SpecificHandler : AmbiguousHandler, IHandler<Both>
	{
		public int Count { get; private set; }
		public ValueTask HandleAsync(Both argument, CancellationToken cancellation = default) => this.HandleAsync(argument, null, cancellation);
		public ValueTask HandleAsync(Both argument, Parameters parameters, CancellationToken cancellation = default) { this.Count++; return ValueTask.CompletedTask; }
	}
	private sealed class UntypedHandler : IHandler
	{
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => throw new InvalidOperationException();
		public ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => throw new InvalidOperationException();
	}
}

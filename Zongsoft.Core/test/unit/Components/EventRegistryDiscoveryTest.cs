using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Collections;

namespace Zongsoft.Components.Tests;

[Collection(nameof(EventRegistryExtensionCollection))]
public class EventRegistryDiscoveryTest
{
	[Fact]
	public void PublicPropertyTypesDeclareEventsWithoutReadingGetters()
	{
		var registry = new AutomaticRegistry();

		Assert.Equal(2, registry.CountInConstructor);
		Assert.Equal(2, registry.Events.Count);
		Assert.Same(StandardEvents.Created, registry["Orders.Created"]);
		Assert.Same(StandardEvents.Updated, registry["Orders.Updated"]);
		Assert.Equal("Automatic:Orders.Created", StandardEvents.Created.QualifiedName);
		Assert.False(registry.Events.Contains("Ignored.Created"));
		Assert.False(registry.Events.Contains("Unexposed.Created"));
		Assert.False(registry.Events.Contains("Direct.Created"));
	}

	[Fact]
	public void InheritedPropertiesAndInheritedGroupFieldsAreDiscovered()
	{
		var registry = new InheritedRegistry();

		Assert.Equal(3, registry.Events.Count);
		Assert.Same(StandardEvents.Created, registry["Orders.Created"]);
		Assert.Same(StandardEvents.Updated, registry["Orders.Updated"]);
		Assert.Same(InheritedEvents.Deleted, registry["Orders.Deleted"]);
	}

	[Fact]
	public void CachedDiscoveryRegistersEachRegistryInstance()
	{
		var first = new AutomaticRegistry();
		var second = new AutomaticRegistry();

		Assert.NotSame(first.Events, second.Events);
		Assert.Equal(2, first.Events.Count);
		Assert.Equal(2, second.Events.Count);
		Assert.Same(StandardEvents.Created, second["Orders.Created"]);
	}

	[Fact]
	public async Task AutomaticAndManualRegistrationPreserveHandlers()
	{
		var registry = new AutomaticRegistry();
		var handler = new Handler();
		Assert.True(registry.Register(StandardEvents.Created.Name, handler));

		try
		{
			registry.Declare(StandardEvents.Created);
			registry.Declare(StandardEvents.Created);

			Assert.Equal(2, registry.Events.Count);
			Assert.Same(handler, Assert.Single(StandardEvents.Created.Handlers));
			Assert.True(await registry.PublishAsync("created"));
			Assert.Equal(1, handler.Count);
			Assert.Equal("created", handler.Argument);
		}
		finally
		{
			registry.Unregister(StandardEvents.Created.Name, handler);
		}
	}

	[Fact]
	public async Task ExtensionsAutomaticallyRegisterWithTheRoot()
	{
		var root = new ManualRegistry();
		var first = new DomainRegistry(root);
		var second = new DomainRegistry(first);
		var handler = new Handler();

		Assert.Equal(2, first.CountInConstructor);
		Assert.Equal(2, second.CountInConstructor);
		Assert.Same(root.Events, first.Events);
		Assert.Same(root.Events, second.Events);
		Assert.Equal(2, root.Events.Count);
		Assert.Equal("Common:Orders.Created", StandardEvents.Created.QualifiedName);
		Assert.True(root.Register(StandardEvents.Created.Name, handler));

		try
		{
			Assert.True(await second.PublishAsync("shared"));
			Assert.Same(root, root.Context.Registry);
			Assert.Equal(1, handler.Count);
			Assert.Equal("shared", handler.Argument);
		}
		finally
		{
			root.Unregister(StandardEvents.Created.Name, handler);
		}
	}

	[Theory]
	[InlineData("Orders.Created", false)]
	[InlineData("orders.created", false)]
	[InlineData("Orders.Created", true)]
	[InlineData("orders.created", true)]
	public void DifferentDescriptorsWithTheSameNameAreRejected(string name, bool differentArgument)
	{
		var registry = new AutomaticRegistry();

		if(differentArgument)
			Assert.Throws<ArgumentException>(() => registry.Declare(new EventDescriptor<int>(name)));
		else
			Assert.Throws<ArgumentException>(() => registry.Declare(new EventDescriptor<string>(name)));

		Assert.Equal(2, registry.Events.Count);
		Assert.Same(StandardEvents.Created, registry[name]);
	}

	[Fact]
	public void AutomaticRegistrationRejectsConflictingRootDeclarations()
	{
		var root = new ManualRegistry();
		root.Declare(StandardEvents.Created);

		Assert.Throws<ArgumentException>(() => new ConflictingRegistry(root));
		Assert.Same(StandardEvents.Created, Assert.Single(root.Events));
	}

	[Fact]
	public void NamedDeclarationsUseTheSameConflictRule()
	{
		var registry = new ManualRegistry();
		var descriptor = registry.Declare("Dynamic.Created");
		registry.Declare((EventDescriptor<string>)descriptor);

		Assert.Same(descriptor, Assert.Single(registry.Events));
		Assert.Throws<ArgumentException>(() => registry.Declare("dynamic.created"));
		Assert.Same(descriptor, Assert.Single(registry.Events));
	}

	[Fact]
	public void NullDescriptorsAreRejectedForAutomaticAndManualRegistration()
	{
		Assert.Throws<ArgumentNullException>("descriptor", () => new NullRegistry());

		var registry = new ManualRegistry();
		Assert.Throws<ArgumentNullException>("descriptor", () => registry.Declare<string>(null));
		Assert.Throws<ArgumentNullException>("name", () => registry.Declare((string)null));
		Assert.Empty(registry.Events);
	}

	private sealed class AutomaticRegistry : EventRegistryBase
	{
		public static readonly EventDescriptor<string> Direct = new("Direct.Created");

		public AutomaticRegistry() : base("Automatic")
		{
			this.CountInConstructor = this.Events.Count;
			this.Event(StandardEvents.Created);
		}

		public int CountInConstructor { get; }
		public StandardEvents Orders => throw new InvalidOperationException("The property getter must not run.");
		public StandardEvents Alias => throw new InvalidOperationException("The alias getter must not run.");
		public static IgnoredEvents Static => throw new InvalidOperationException();
		private IgnoredEvents Private => throw new InvalidOperationException();
		public IgnoredEvents this[DateTime date] => throw new InvalidOperationException();
		public void Declare<TArgument>(EventDescriptor<TArgument> descriptor) => this.Event(descriptor);
		public ValueTask<bool> PublishAsync(string argument) => this.RaiseAsync(StandardEvents.Created.Name, argument);

		public sealed class UnexposedEvents
		{
			public static readonly EventDescriptor<string> Created = new("Unexposed.Created");
		}
	}

	private class BaseRegistry() : EventRegistryBase("Inherited")
	{
		public StandardEvents Orders => throw new InvalidOperationException();
	}

	private sealed class InheritedRegistry : BaseRegistry
	{
		public InheritedEvents ExtendedOrders => throw new InvalidOperationException();
	}

	private sealed class ManualRegistry() : EventRegistryBase("Common")
	{
		public EventContext Context { get; private set; }
		public void Declare<TArgument>(EventDescriptor<TArgument> descriptor) => this.Event(descriptor);
		public EventDescriptor Declare(string name) => this.Event<string>(name);
		protected override EventContext<TArgument> GetContext<TArgument>(string name, TArgument argument, Parameters parameters)
		{
			var context = base.GetContext(name, argument, parameters);
			this.Context = context;
			return context;
		}
	}

	private sealed class DomainRegistry : EventRegistryBase
	{
		public DomainRegistry(EventRegistryBase registry) : base(registry) => this.CountInConstructor = this.Events.Count;
		public int CountInConstructor { get; }
		public StandardEvents Orders => throw new InvalidOperationException();
		public ValueTask<bool> PublishAsync(string argument) => this.RaiseAsync(StandardEvents.Created.Name, argument);
	}

	private sealed class ConflictingRegistry(EventRegistryBase registry) : EventRegistryBase(registry)
	{
		public ConflictingEvents Orders => throw new InvalidOperationException();
	}

	private sealed class NullRegistry() : EventRegistryBase("Null")
	{
		public NullEvents Orders => throw new InvalidOperationException();
	}

	private class StandardEvents
	{
		public static readonly EventDescriptor<string> Created = new("Orders.Created");
		public static readonly EventDescriptor<string> Updated = new("Orders.Updated");
		public static EventDescriptor<string> Mutable = new("Excluded.Mutable");
		public readonly EventDescriptor<string> Instance = new("Excluded.Instance");
		public static readonly EventDescriptor NonGeneric = new("Excluded.NonGeneric");
		public static readonly object Untyped = new EventDescriptor<string>("Excluded.Untyped");
		private static readonly EventDescriptor<string> _private = new("Excluded.Private");
		public static EventDescriptor<string> Property => throw new InvalidOperationException();
	}

	private sealed class InheritedEvents : StandardEvents
	{
		public static readonly EventDescriptor<string> Deleted = new("Orders.Deleted");
	}

	private sealed class IgnoredEvents
	{
		public static readonly EventDescriptor<string> Created = new("Ignored.Created");
	}

	private sealed class ConflictingEvents
	{
		public static readonly EventDescriptor<int> Created = new("Orders.Created");
	}

	private sealed class NullEvents
	{
		public static readonly EventDescriptor<string> Created = null;
	}

	private sealed class Handler : HandlerBase<string>
	{
		public int Count { get; private set; }
		public string Argument { get; private set; }
		protected override ValueTask OnHandleAsync(string argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Count++;
			this.Argument = argument;
			return ValueTask.CompletedTask;
		}
	}
}

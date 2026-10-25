# Event dispatch and ambient features

`EventExchanger` coordinates outbound delivery and local reception. `IEventDispatcher` decides when execution is allowed, `IExecutor<EventContext>` executes delivery, and `IEventChannel` handles transport. The exchanger explicitly implements `IEventReceiver.ReceiveAsync()`. Channels replay incoming events through this interface without exchanging them again.

## Event declarations and argument contracts

Declare one descriptor per event name. For non-generic `EventDescriptor`, list the permitted argument types in its constructor; `ArgumentTypes` exposes a read-only copy. The constructor without a type list declares the `object` contract. Types must be closed and distinct, and their transport signatures must also be unique.

```csharp
public sealed class InventoryEvents : EventRegistryBase
{
	public InventoryEvents() : base("Inventory") => this.Items = new ItemEvents(this);
	public ItemEvents Items { get; }

	public sealed class ItemEvents(InventoryEvents registry)
	{
		public static readonly EventDescriptor Created = new("Item.Created",
			[typeof(string), typeof(IEnumerable<string>)]);

		public ValueTask<bool> OnCreatedAsync(string argument, Parameters parameters, CancellationToken cancellation = default) =>
			registry.RaiseAsync(Created, argument, parameters, cancellation);
		public ValueTask<bool> OnCreatedAsync(IEnumerable<string> argument, Parameters parameters, CancellationToken cancellation = default) =>
			registry.RaiseAsync(Created, argument, parameters, cancellation);
	}
}
```

A single argument contract can also use `EventDescriptor<TArgument>`, which declares `[typeof(TArgument)]` through the base class. The generic argument can be an individual or collection type; use the non-generic form when an event accepts both. Generic constructors accept a name, title, and description, with an additional overload taking `IPredication<TArgument>` first:

```csharp
public static readonly EventDescriptor<string> Updated = new(
	Predication.Predicate<string>(argument => !string.IsNullOrEmpty(argument)), "Item.Updated");
```

This declaration uses the same predicate and dispatch pipeline as `new EventDescriptor("Item.Updated", [typeof(string)]) { Predication = ... }`. Typed `HandleAsync` overloads on the generic descriptor always dispatch as `TArgument`, including arrays, derived objects, and null. The registry's `Event<TArgument>(name, title, description)` returns the generic descriptor; `RaiseAsync<TArgument>` continues to select the publication's declared argument type.

Event declarations normally omit the constructor's `title` and `description` arguments and avoid reading resource strings in property initializers. Each read of `Title` looks up `{EventName}.Title`, then `{EventName}`; `Description` looks up `{EventName}.Description`, using the current `CurrentUICulture`. For example, use `Item.Updated.Title` and `Item.Updated.Description`. Resources belong to the assembly of the registry that declares the event. An extension registry shares the root's event collection and delivery pipeline while resolving resources from its own assembly. Once an ASP.NET host sets the request culture, the same static descriptor can return text for the current request. An explicit non-null title or description, including an empty string, is a fixed override that disables resource fallback for that property.

Registries discover public static readonly fields whose declared type is assignable to `EventDescriptor` on their public instance property types, including generic descriptors, derived types, and inherited members, without invoking property getters. Registering the same instance again is ignored. Different descriptors with the same case-insensitive name are rejected. Lookup, counting, and subscriptions operate on event names; adding an argument contract does not add an event.

Publication selects the declared type of `RaiseAsync<TArgument>`, which must appear exactly in the descriptor. An array passed through the collection overload retains the `IEnumerable<T>` contract, and null retains its declared type. Runtime types, collection lengths, and conversions do not select contracts. Explicit dispatch uses `HandleAsync(Type, object, Parameters, CancellationToken)`; `EventLocator` reads the event name and declared type from executor parameters `name` and `argumentType`.

Handlers implement `IHandler<T>`, usually through `HandlerBase<T>`. Each instance uses an exact contract when available; otherwise it uses the unique most specific contract capable of accepting the declared type. Incomparable candidates fail at registration. All distinct compatible handler instances run; registering the same instance repeatedly still invokes it once. Incompatible handlers and handlers implementing only non-generic `IHandler` cannot be attached. Dispatch does not convert arguments to make handlers match.

Single and collection handlers can share a plugin path such as `/Workbench/Modules/Inventory/Events/Item/Created`. A publication uses its selected contract without wrapping singles or splitting batches. Filters inspect `EventContext.Descriptor`, `ArgumentType`, and `GetArgument()`. Calling a descriptor's `HandleAsync` runs its predicate and handlers; registry publication also runs filters and remote exchange.

## Transport and snapshots

The message topic contains only the qualified event name. The JSON envelope contains `Signature`, `Argument`, and `Parameters`. `Signature` is the declared type alias (`TypeAlias.GetAlias`) cached when the descriptor is constructed, without assembly versions. Both endpoints require matching contracts and type-alias configuration. Receivers match signatures only against the corresponding descriptor's declarations; they do not pass incoming signatures to a type loader. Missing, unknown, or incompatible signatures are rejected. Envelopes without signatures are unsupported.

`Events.Marshaler.Unmarshal` returns `(Type argumentType, object argument, Parameters parameters)`. Custom channels pass this declared type to `IEventReceiver.ReceiveAsync(name, argumentType, argument, parameters, cancellation)`. Remote reception and deferred snapshots restore `EventContext<TArgument>`, including interface contracts and null arguments. Local and remote dispatch use the same handler selection rules. Reception runs local handlers without exchanging the event again.

## Queue event conventions

Core's `Events.Channel.cs` supplies the topic convention and incoming message handler shared by queue event channels:

- `Events.GetTopic()` returns the logical root `Events`; supplying a qualified event name returns `Events/qualified-name`.
- `Events.GetHandler(receiver)` creates an independent `IHandler<Message>` bound to that receiver. It validates the logical `Events/` topic boundary, uses `Events.Marshaler` and the current application's event registrations to restore the argument and parameters, then calls `IEventReceiver.ReceiveAsync()`.

Empty messages, unrelated topics, and topics without an event name are ignored. The returned task covers the receiver's asynchronous execution. Deserialization errors, receive failures, and cancellation propagate to the caller. The handler neither exchanges events again nor acknowledges messages automatically.

The root contains no queue group prefix or subscription wildcard. Each adapter handles its queue's subscription matching rules and supplies logical topics to the common handler. `ZeroQueueEventChannel` uses the root for prefix subscriptions and the full event topic for publishing, reusing both Core entry points. Filtering, queue options, and subscription lifetime belong to the channel.

## Ambient features and execution policies

`IFeatureProvider.GetFeatures()` captures the calling environment. Returned features must remain bound to that environment. `IExecutorContext.Features` holds a read-only collection snapshot while preserving feature identity; it does not configure an execution pipeline.

A runtime feature combines `IFeature` with `Zongsoft.Common.IPredication`. Its `PredicateAsync()` method returns the environment's final decision: `true` permits execution, `false` discards the event, and an incomplete result defers execution. Every condition must allow execution; any denial discards the event. Duplicate references are evaluated once, while distinct instances of the same type remain independent. Conditions must support repeated and concurrent evaluation without looking up the current environment again. The dispatcher passes the event context and its `Parameters` to the parameterized overload, evaluating each feature once per dispatch. Conditions may return a shared completion task. The dispatcher evaluates with `CancellationToken.None` and applies the event cancellation token only while waiting for the result, preserving the shared decision and allowing completed conditions to drain on stop. Direct callers of `IPredication` may cancel their own evaluation; implementations must not propagate that cancellation to the shared environment. A false result discards the event rather than skipping the feature. Unsupported runtime features fail explicitly.

`IFeature` identifies a feature; it does not require an application or condition-evaluation method. Runtime conditions opt into `IPredication`. `IFeatureBuilder` configures retry, timeout and fallback policies through `Executor.Pipelines`. The dispatcher consumes ambient features without passing them to that builder. Plain `Executor.Build(...)` does not consult the pipeline builder. Ordinary executors do not automatically capture ambient features or defer execution.

## Feature providers

`FeatureProvider` composes `IFeatureProvider` sources and captures a fixed feature snapshot. `FeatureProvider.Default.Providers` includes the shared instance exposed by `Transaction.Features`. The default composition also resolves `IFeatureProvider` services from the current application on each capture. Transaction support works in standalone applications without application services, Core assembly scanning, or plugin manifest declarations.

Applications can append shared providers with `FeatureProvider.Default.Providers.Add(provider)` or register provider services. Collection mutations are synchronized; registrants own the providers and their resources. Removing a source from `Providers` removes that explicit source; independently registered service sources still participate.

Each capture snapshots the sources of all nested `FeatureProvider` compositions before calling any source's `GetFeatures()`. Sources and returned features are deduplicated by reference, preserving their first occurrence. Separate instances of the same type remain independent. Null sources are ignored; repeated or cyclic composition references are expanded only once. Capture callbacks run outside collection locks. Their collection mutations affect subsequent captures only. Returned features remain bound to the captured environment.

`EventDispatcher` accepts one `IFeatureProvider`; `null` selects `FeatureProvider.Default`. An explicitly supplied provider owns all sources. A plain `new FeatureProvider()` is empty and does not consult shared sources or application services. To extend the default behavior for a particular dispatcher, compose it explicitly:

```csharp
var provider = new FeatureProvider(FeatureProvider.Default, customProvider);
await using var dispatcher = new EventDispatcher(provider);
await dispatcher.StartAsync();
```

This method-body fragment requires `Zongsoft.Components` and an application-supplied `customProvider`. A standalone `new EventDispatcher()` uses the default composition directly. `Executor` provides execution construction and policy composition; it does not own ambient providers.

## Transaction provider

`Transaction.Features` exposes a private provider that captures the current root transaction. `TransactionContext.Feature.cs` contains a private feature implementing `IFeature` and `IPredication`. After all enlisted callbacks finish and before user `Completed` handlers run, it maps `Committed` to permission and both `Aborted` and `Undetermined` to denial. The transaction condition ignores the event and additional parameters and depends only on its captured root transaction. Its decision is available before transaction completion returns; delivery continues asynchronously under dispatcher ownership.

A root transaction and its children share the same feature instance. The feature remains bound to that transaction rather than following later ambient changes. Repeated and concurrent waits observe the same completion; canceling one wait does not cancel the transaction or other waits. Committing does not wait for remote delivery.

## Admission and lifetime

For immediate execution, `DispatchAsync` completes with the execution result. For deferred execution, completion means admission. Before returning, the dispatcher snapshots the event argument and parameters with `Events.Marshaler`, then restores them once before delivery. Snapshot failures propagate to the caller.

The default capacity is 1024 unfinished work items, including executing deliveries. Full admission throws `InvalidOperationException` immediately instead of waiting for a transaction to free capacity. Work sharing an ambient condition preserves admission order. Independent conditions can become ready independently. The configured policy pipeline starts only after admission conditions permit execution. Every delivery attempt passes through executor filters, retaining captured ambient features and shared execution parameters while using the cancellation token supplied by the pipeline.

Background delivery does not inherit the original ambient transaction or request services. Immediate failures propagate; deferred failures are observed and logged without breaking subsequent work. Caller cancellation can cancel waiting and delivery; executors and channels must cooperate with the token.

`EventDispatcher` uses `WorkerBase` for lifecycle management. Stopping seals admission, discards work whose conditions remain unresolved, and drains permitted deliveries. The exchanger then closes channels. Restart creates a separate dispatch session. Canceling a stop caller's wait does not finish delivery; an executor that never finishes also prevents stopping from completing.

Configure `EventExchanger.Dispatcher` while the exchanger is stopped. The exchanger owns a fixed private executor for channel delivery. Set `EventDispatcher.Pipeline` only while the dispatcher is stopped; `null` means direct execution. Keep the channel collection stable during lifecycle operations and delivery. The exchanger coordinates component lifetimes; the composition owner performs final disposal. Channels can reopen after `CloseAsync` but cannot reopen after disposal. The ZeroMQ channel creates a subscription on open and disposes it on close, while borrowing the injected queue.

## Optional delivery policies

The Polly plugin supplies `Executor.Pipelines`; application plugins explicitly compose delivery policies. This fragment uses the default `EventDispatcher` after Polly initialization while both exchanger and dispatcher are stopped, with `Zongsoft.Components.Features` imported:

```csharp
var dispatcher = (EventDispatcher)EventExchanger.Instance.Dispatcher;
var features = Executor.Features
	.Retry(TimeSpan.FromMilliseconds(100), false, 2)
	.Timeout(TimeSpan.FromSeconds(3));

dispatcher.Pipeline = Executor.Pipelines.Build<EventContext>(features.Build());
```

An application plugin can assign an `IFeaturePipeline<EventContext>` to `EventDispatcher.Pipeline` through `object.property`, then attach that dispatcher through the exchanger's `Dispatcher` property. Policies cover delivery only, excluding the ambient wait. The pipeline instance is reused across deliveries and restarts to retain circuit-breaker and rate-limiter state. It must support concurrent calls; its composition owner handles disposal. Each start fixes the pipeline for that dispatch session. With no configured pipeline, the dispatcher does not consult the global pipeline builder; loading Polly alone preserves default delivery.

This example retries the entire channel fan-out, so channels that already succeeded can receive duplicates. Policies can also be composed in individual channel adapters or decorators. ZeroMQ's default `MostOnce` result for no matching subscriber is not an exception and does not trigger exception retries. Polly's current void fallback invokes its callback while preserving the original exception.

In-memory deferral determines when to send after commit. It does not provide a durable outbox, atomic database/message commit, or exactly-once delivery.

## Remote publication scope

`EventExchanger.Filtering` uses `EventFiltering` before invoking the dispatcher. Excluded events do not create deferred environment snapshots or enter channel serialization; an exchanger without channels also skips dispatch. Local handlers, registry filters and reception are unaffected. The main plugin exposes `/Workbench/Events/Exchanger/Filtering` for deployment configuration. Assemble rules before startup; concurrent mutation is unsupported.

The first matching entry wins. Empty rules and unmatched events allow publication. For an allowlist, place inclusive entries such as `Inventory:Item.Created` first and end with `!` to exclude everything else. `*` matches entire registry or event names, not event-name prefixes. Channel-specific filtering can further restrict delivery. Remote configuration does not determine local business needs or discover remote handlers or online status; data services must also explicitly select operations to prepare and publish.

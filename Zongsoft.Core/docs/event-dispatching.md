# Event dispatch and ambient features

`EventExchanger` coordinates outbound delivery and local reception. `IEventDispatcher` decides when execution is allowed, `IExecutor<EventContext>` executes delivery, and `IEventChannel` handles transport. The exchanger explicitly implements `IEventReceiver.ReceiveAsync()`. Channels replay incoming events through this interface without exchanging them again.

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

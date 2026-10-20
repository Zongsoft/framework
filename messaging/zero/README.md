# Zongsoft.Messaging.ZeroMQ Message Queue Plugin

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.ZeroMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

<a name="abstract"></a>
## Abstract

Zongsoft.Messaging.ZeroMQ is a [NetMQ](https://github.com/zeromq/netmq)-based adapter for the messaging and communication abstractions in [Zongsoft.Core](../../Zongsoft.Core). It provides topic publishing and subscription through `IMessageQueue`, and also supplies request/response and event-channel adapters.

The included `ZeroQueueServer` combines an XPUB/XSUB exchange for at-most-once traffic with a durable acknowledgement path for at-least-once traffic. Clients discover the current Broker epoch and all runtime endpoints automatically.

<a name="features"></a>
## Features

- Implements the Zongsoft `IMessageQueue`, `IRequester`, `IResponder`, and `IEventChannel` abstractions;
- Supports multiple publishers and subscribers through an XPUB/XSUB exchange;
- Supports topic prefixes, optional message groups, instance filtering, and heartbeats;
- Supports Brotli, GZip, ZLib, or Deflate payload compression above a configurable threshold;
- Supports immediate at-most-once broadcast and Broker-persisted, explicitly acknowledged competing at-least-once delivery;
- Supports standalone use and Zongsoft plugin-based hosting;
- Targets .NET 8, .NET 9, and .NET 10.

<a name="installation"></a>
## Installation

Install the NuGet package:

```shell
dotnet add package Zongsoft.Messaging.ZeroMQ
```

To build from this repository, build Zongsoft.Core first:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj
dotnet build messaging/zero/Zongsoft.Messaging.ZeroMQ.slnx
```

<a name="topology"></a>
## Exchange Topology

| Endpoint | Default in packaged configuration | Purpose |
| --- | :---: | --- |
| Discovery | `7969` | Clients request the Broker epoch and runtime endpoint ports. |
| Reliability control | `32100` | Reliable subscription registration, delivery, and acknowledgement. |
| Publisher incoming | `32101` | Application publishers connect here. |
| Subscriber outgoing | `32102` | Application subscribers connect here. |

The server uses explicit fixed addresses. A directly constructed server defaults to Discovery 7969, Incoming 32101, and Outgoing 32102, with Control and Storage unset. The daemon configuration explicitly enables Control 32100. Clients rediscover runtime ports after a stopped server changes addresses and restarts.

The server binds each role to its configured IP and does not configure authentication or encryption. NetMQ IPv6 mode may also accept IPv4 through the [::] wildcard; it does not promise IPv6-only wildcard binding. Restrict access at the host or network boundary, or add an authenticated transport before using it across an untrusted network.

<a name="configuration"></a>
## Configuration

### Server

The [daemon option file](src/Zongsoft.Messaging.ZeroMQ-daemon.option) supplies both server settings and binding declarations:

```xml
<?xml version="1.0" encoding="utf-8" ?>

<?zongsoft.tools.binding name="zeromq/control"
	path="Messaging/ZeroMQ/Server"
	filter="enabled:true"
	template="${this.control}"
	protocols="tcp"?>
<?zongsoft.tools.binding name="zeromq/incoming"
	path="Messaging/ZeroMQ/Server"
	filter="enabled:true"
	template="${this.incoming}"
	protocols="tcp"?>
<?zongsoft.tools.binding name="zeromq/outgoing"
	path="Messaging/ZeroMQ/Server"
	filter="enabled:true"
	template="${this.outgoing}"
	protocols="tcp"?>
<?zongsoft.tools.binding name="zeromq/discovery"
	path="Messaging/ZeroMQ/Server"
	filter="enabled:true"
	template="${this.discovery}"
	protocols="tcp"?>

<options>
	<option path="/Messaging/ZeroMQ">
		<server name="QueueServer" enabled="true"
		        storage="QueueServer"
		        control="tcp://0.0.0.0:32100"
		        incoming="tcp://0.0.0.0:32101"
		        outgoing="tcp://0.0.0.0:32102"
		        discovery="tcp://0.0.0.0:7969" />
	</option>
</options>
```

Discovery, Incoming, and Outgoing are required. Configure Control and Storage together to enable reliable delivery; the Storages factory must create the named storage. Missing dependencies or failed creation prevent startup. Omit or empty both fields for broadcast only. Name identifies the broker, while Storage selects an independent storage configuration. Enabled=false prevents startup and excludes all four bindings; an empty Control produces no binding.

Addresses require tcp, a literal IP address or IPv4 wildcard *, and an explicit port from 1 through 65535. Bracket IPv6 addresses. DNS names, non-root paths, queries, fragments, and random ports are rejected. Each role must use a distinct port. Addresses and storage settings can change only while stopped; startup arguments do not override them. The protocols="tcp" values are constant templates describing the TCP transport.

Discovery advertises ports only; clients reuse the discovery host. All roles must therefore be reachable through that host at the same advertised ports. Separate listening IPs do not advertise separate hosts or support arbitrary NAT port translation.

The [standalone server sample](samples/server/Program.cs) starts a loopback broadcast server without persistence:

```csharp
await using var server = new ZeroQueueServer()
{
	Discovery = "tcp://127.0.0.1:7969",
	Incoming = "tcp://127.0.0.1:32101",
	Outgoing = "tcp://127.0.0.1:32102",
};
await server.StartAsync([]);
```

### Client Connection

Define a `ZeroMQ` connection under `/Messaging/ConnectionSettings`:

The client name and group come from the [sample client](samples/client/Program.cs), expressed here in host option format:

```xml
<configuration>
	<option path="/Messaging">
		<connectionSettings default="ZeroMQ">
			<connectionSetting connectionSetting.name="ZeroMQ"
			                   driver="ZeroMQ"
			                   value="server=127.0.0.1;port=7969;group=Demo;client=Zongsoft.Messaging.ZeroMQ.Sample;" />
		</connectionSettings>
	</option>
</configuration>
```

| Setting | Default | Description |
| --- | --- | --- |
| `Server` | required | Host name or IP address of the discovery endpoint. Do not include `tcp://`. |
| `Port` | `7969` | Discovery endpoint port. |
| `Topic` | empty | Topic used when `ProduceAsync` or `SubscribeAsync` omits a topic. |
| `Group` | empty | Prefix added as `Group:Topic` to isolate applications sharing an exchange. |
| `Client` | empty | Stable client name used as part of an automatically generated instance identifier. |
| `Instance` | generated | Explicit producer instance identifier. Empty or `*` generates a unique identifier. |
| `Filter` | excludes self | Comma-separated instance filter controlling which producers are accepted. |
| `Timeout` | `10s` | Discovery and subscription-synchronization timeout. |
| `Heartbeat` | `10s` | Heartbeat interval. A value less than or equal to zero disables heartbeats. |
| `ReconnectInterval` | `1s` | Minimum interval between endpoint rediscovery attempts. |

The default filter excludes messages produced by the same queue instance. Use `Filter=*` to accept every instance, `Filter=.` (or `~`) to accept only the current instance, ordinary identifiers as an allow list, and `!identifier` entries as exclusions.

[`ZeroConnectionSettings`](src/Configuration/ZeroConnectionSettings.cs) has no `TOptions` constraint: unknown text entries remain in the main dictionary, and the indexer accepts arbitrary parameter names. The driver reads the parameters it uses through typed properties.

The defaults in the table apply to typed reads. For example, when `Port` is absent, `settings.Port` returns `7969`, while `settings["Port"]` returns `null`; the default is not inserted into `Value`. `Value` retains main parameters and explicit extension properties. See the [Core settings contract](../../Zongsoft.Core/docs/settings.md) for the complete rules.

<a name="usage"></a>
## Usage

### Publish and Subscribe

In a host with the ZeroMQ plugin deployed, obtain the shared queue by provider and connection name. This uses the `ZeroMQ` connection above; for this in-process loopback example set `filter=*` on that connection and start a compatible broker beforehand. The consumer module references only Core:

```csharp
using System.Text;
using Zongsoft.Services;
using Zongsoft.Messaging;

var provider = ApplicationContext.Current.Services
	.FindRequired<IMessageQueueProvider>("ZeroMQ");
var queue = provider.Queue("ZeroMQ");
var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
var topic = $"docs/{Guid.NewGuid():N}";

var consumer = await queue.SubscribeAsync(topic, message =>
	received.TrySetResult(Encoding.UTF8.GetString(message.Data.Span)));

try
{
	var identifier = await queue.ProduceAsync(topic, "demo".AsMemory());
	if(identifier == null)
		throw new InvalidOperationException("No matching subscription was visible.");

	Console.WriteLine(await received.Task.WaitAsync(TimeSpan.FromSeconds(10)));
}
finally
{
	await consumer.UnsubscribeAsync();
}
```

The provider reuses named queues; business operations must not dispose a shared queue. This example cancels only its own unique-topic subscription and waits for its handler before exiting. A non-null publish identifier still does not mean business processing completed. A standalone tool that constructs `ZeroQueue` directly owns its entire lifetime.

Subscriptions use prefix matching. One `ZeroQueue` keeps one consumer for each logical topic. A repeated subscription reuses that consumer only when the handler, tags, and subscription options match; a mismatch throws `InvalidOperationException` instead of replacing the existing subscription. With `Group=Demo`, the physical wire topic is `Demo:topic/reliable`, while handlers receive the logical `Message.Topic` value `topic/reliable`.

Each subscriber invokes its handler sequentially in receive order. When its bounded pending queue reaches capacity, that subscriber pauses Poller reads and resumes after the handler frees capacity; other sockets remain responsive.

### Compression

Set `MessageEnqueueOptions.Compression` to an algorithm and the minimum payload size that enables compression. `MessageCompression.Value` is an integer byte threshold; its text format is `<algorithm>:<threshold>`. Zero compresses every non-empty payload and the default value disables compression:

```csharp
var options = new MessageEnqueueOptions()
{
	Compression = MessageCompression.Parse("Brotli:4096"),
};

await queue.ProduceAsync("documents/updated", payload, options);
```

The equivalent strongly typed construction is `new MessageCompression("Brotli", 4096)`.

### At-least-once delivery

Set `LeastOnce` on both the subscription and publication. Only the Broker requires an `IMessageStorage`; publishers do not persist messages. A handler must call `AcknowledgeAsync`; returning normally is not an acknowledgement.

The following is from [ZeroQueueReliabilityTests.LeastOnceCompletesAfterBrokerAcceptanceBeforeAcknowledge](test/ZeroQueueReliabilityTests.cs). `ReliableServerScope`, `CreateQueue` and `AcknowledgingHandler` are real fixtures in that test file. Read or run them in the original test project; they are not application plugin types:

```csharp
await using var scope = await ReliableServerScope.StartAsync();
using var publisher = CreateQueue(scope.Port, "publisher", "publisher");
using var subscriber = CreateQueue(scope.Port, "subscriber", "subscriber");
var handler = new AcknowledgingHandler(1, false);
await subscriber.SubscribeAsync("topic/reliable", handler, ReliableSubscribeOptions());

var identifier = await publisher.ProduceAsync("topic/reliable", Encoding.UTF8.GetBytes("reliable"), ReliableEnqueueOptions()).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
var message = await handler.ReceiveAsync(TimeSpan.FromSeconds(5));

Assert.False(string.IsNullOrWhiteSpace(identifier));
Assert.Equal(identifier, message.Identifier);
Assert.Single(await GetPendingAsync(scope));

await message.AcknowledgeAsync();
Assert.True(await WaitForPendingCountAsync(scope, 0, TimeSpan.FromSeconds(5)));
```

The fixture checks that Pending remains before acknowledgement and is removed afterwards. It is not an order-storage implementation or proof of idempotent business effects. Its in-process test storage does not establish production restart durability.

The Broker accepts a publication only when an online matching subscription exists. No match returns `null` without writing Storage. With a match, the Broker persists Pending first and then returns the identifier. Delivery competes among online subscribers; any one acknowledgement removes Pending. Retries reuse `Message.Identifier` and may choose another consumer, so handlers must be idempotent.

Set `ZeroQueueServer.Storage` and `Storages` only while stopped. With Control enabled, each start calls `Storages.Create(Storage)` and the server owns the result. Stop, failed-start cleanup and disposal release owned storage, preferring `IAsyncDisposable`. Restart after a successful stop creates a new storage instance while retaining the factory and configuration. Failed cleanup retains remaining resources for an explicit retry; once disposal begins, only another `DisposeAsync` may retry cleanup. When Control and Storage are unset, no factory is called and discovery includes only Incoming and Outgoing; LeastOnce is unavailable.

| Messaging option | Support |
| --- | --- |
| `Compression` | Supported by both `MostOnce` and `LeastOnce` with Brotli, GZip, ZLib, or Deflate; only `Message.Data` is compressed. |
| Tags and identity | Both modes carry `Identifier`, `Identity`, and `Tags` as independent metadata. |
| `Delay` | Unsupported; Core checks `Features` and rejects a positive delay before entering the driver. |
| Expiration | Supported by `LeastOnce`; zero means no expiration. |
| Priority | Not implemented. |
| `MostOnce` | Supported; returns `null` when no subscription is visible at send time, otherwise sends locally once. |
| `LeastOnce` | Supported with Broker persistence, competing consumers, explicit acknowledgement, and same-identifier retry. |
| `ExactlyOnce` | Not supported and fails before transport state is created. |
| Subscription fallback | Not implemented by the current handler dispatcher. |

### Request and Response

`ZeroRequester` and `ZeroResponder` exchange requests through logical URL topics; the default reply topic is `<url>/reply`. The real [ZeroRequesterTests.RequesterReceivesImmediateResponses](test/ZeroRequesterTests.cs) starts separate requester/responder queues, registers the [EchoHandler fixture](test/ZeroTestUtility.cs), sends to `rpc/echo`, and checks payload and request-identifier correlation. It stops/disposes the responder in `finally` and also disposes each request token.

One request can receive multiple replies. Each reply refreshes the ten-minute pending-cache expiry; a `Replaced` notification preserves the active token. Actual expiration removes its registration, while disposing the token also clears buffered replies. Dispose tokens when finished collecting replies. [ZeroRequesterMultipleReplyTests](test/ZeroRequesterMultipleReplyTests.cs) covers 3/8 replies over real sockets and deterministic cache replacement, expiration, late replies, and token/requester disposal. Set `ZONGSOFT_MESSAGING_TESTS=true` to enable the socket tests.

[ZeroResponderTests](test/ZeroResponderTests.cs) additionally checks subscription rollback on startup failure. No nonexistent `PingHandler` is supplied here. Applications use shared communication contracts and host composition; standalone tests own their directly constructed instances.

### Event Channel

`ZeroQueueEventChannel` connects an `EventExchanger` to queue topics under `Events/...`. Core supplies the topic convention through `Events.GetTopic()` and restores incoming messages through `Events.GetHandler(receiver)`, forwarding them to `IEventReceiver`. The channel manages filtering, queue options, and subscription lifetime:

```csharp
await using var channel = new ZeroQueueEventChannel(queue);
await channel.OpenAsync(exchanger);
await channel.SendAsync(eventContext);
```

The plugin creates `ZeroQueueEventChannel` directly at `/Workbench/Events/Exchanger/Channels/ZeroMQ` and adds it to the exchanger. Its `Filtering` child exposes the channel's filtering instance at `/Workbench/Events/Exchanger/Channels/ZeroMQ/Filtering`. Early references to either node share the same instance and append the channel once. Channels can reopen after closing; closing disposes the subscription while leaving the borrowed queue alive. Request/response and event channels support `Group`, applying the prefix only at the network boundary and using logical topics inside the adapter.

Core's `EventDispatcher` handles ambient deferral: remote delivery starts after commit, while rollback or an undetermined outcome discards events. Default ZeroMQ delivery does not apply Polly policies. An application plugin can explicitly compose retry, timeout or fallback through `EventDispatcher.Pipeline`. See [event dispatch and ambient features](../../Zongsoft.Core/docs/event-dispatching.md).

<a name="semantics"></a>
## Delivery Semantics

The selected `MessageReliability` determines the contract:

- `MostOnce` is transient broadcast. If the application XPUB sees no matching subscription at send time, `ProduceAsync` returns `null` without sending. Otherwise it sends locally once and returns a unique identifier. Every broadcast recipient sees that identifier, but it is not remote or Handler acknowledgement.
- `LeastOnce` returns `null` without persistence when no online matching subscription exists. Otherwise the Broker persists Pending first and immediately returns the unique identifier without waiting for a Handler acknowledgement.
- The Broker selects one online consumer per attempt. It retries the same identifier until any valid acknowledgement removes Pending. If all consumers disconnect after acceptance, Pending remains until a subscription returns.
- `LeastOnce` permits duplicate handler invocations. It does not deduplicate business effects and does not provide exactly-once delivery.
- A Control timeout, caller cancellation, or disconnect may leave the publisher unable to determine whether the Broker accepted the message. These stop only local waiting and cannot revoke acceptance already in progress; a business retry can still produce a duplicate.
- Expired reliable messages are removed from Broker Pending with a diagnostic record.
- A queue snapshots its connection, ports, group, filter, timeout, and heartbeat settings at construction; mutating the original settings object does not reconfigure a running queue;
- Empty business payloads are supported.

Message storage is an independent plugin concept, not part of the ZeroMQ driver. This package provides no default file store. Applications using `LeastOnce` inject an `IMessageStorageFactory`; the factory selects the connection named by Server.Storage and creates its exclusive storage. Storage supports exact-topic reads and clears. An implementation must hold a message snapshot before `SetAsync` returns and provide the required restart durability.

See the [ZeroMQ 1.0 protocol](PROTOCOL.md) for the complete Discovery, Broadcast, and Control frame definitions.

<a name="samples"></a>
## Samples and Troubleshooting

The [.NET 10 samples](samples) contain an interactive exchange server and client. Start the server first, then run one client as a subscriber and another as a publisher. See the [sample guide](samples/README.md) for commands.

If messages are not received:

1. Verify that discovery, control, incoming, and outgoing ports are reachable in the required directions;
2. Verify that publisher and subscriber use the same `Group` and compatible topic prefixes;
3. Check the `Filter` setting—self-produced messages are excluded by default;
4. If `ProduceAsync` returns `null`, verify that a matching subscription was visible to the Broker at that instant and apply the application's retry policy if appropriate;
5. For `LeastOnce`, verify Server `Storage`, the Control endpoint, explicit acknowledgement, and expiration;
6. Inspect Broker Pending data, online subscriptions, and consumer idempotency when reliable delivery remains unresolved.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The deployment manifest selects the daemon plugin, matching option file, and Storage-daemon plugin together for site=daemon. Client-only hosts load the main plugin and client settings. The Storage plugin depends on Daemon and attaches the factory at /Workspace/Messaging/Storages/Redis; change that contribution to use another factory. Reliable delivery requires this factory and the connection selected by Server.Storage.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Messaging.ZeroMQ.Daemon` | [Zongsoft.Messaging.ZeroMQ-daemon.plugin](src/Zongsoft.Messaging.ZeroMQ-daemon.plugin) |
| `Zongsoft.Messaging.ZeroMQ` | [Zongsoft.Messaging.ZeroMQ.plugin](src/Zongsoft.Messaging.ZeroMQ.plugin) |
| `Zongsoft.Messaging.ZeroMQ.Storage` | [Zongsoft.Messaging.ZeroMQ.Storage-daemon.plugin](src/Zongsoft.Messaging.ZeroMQ.Storage-daemon.plugin) |
| File copying and dependencies | [Zongsoft.Messaging.ZeroMQ.deploy](src/Zongsoft.Messaging.ZeroMQ.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft messaging zeromq]
nuget:Zongsoft.Messaging.ZeroMQ
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Messaging.ZeroMQ.option`, `Zongsoft.Messaging.ZeroMQ.plugin`, `Zongsoft.Messaging.ZeroMQ-${site}.plugin`, `Zongsoft.Messaging.ZeroMQ-${site}.option`, `Zongsoft.Messaging.ZeroMQ.Storage-${site}.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

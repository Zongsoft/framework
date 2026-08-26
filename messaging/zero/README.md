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

`7969` is the built-in discovery-port default. Omitted runtime ports are selected dynamically and rediscovered after a Broker restart. Fixed control, incoming, and outgoing ports are still recommended for predictable firewall and operations configuration.

The server binds TCP endpoints on all network interfaces and does not configure authentication or encryption. Restrict access at the host or network boundary, or add an authenticated transport before using it across an untrusted network.

<a name="configuration"></a>
## Configuration

### Server

The packaged daemon plugin starts `ZeroQueueServer` automatically. Configure its data endpoints under `/Messaging/ZeroMQ/Servers`:

```xml
<configuration>
	<option path="/Messaging/ZeroMQ">
		<servers port="32100,32101,32102">
			<server server.name="unnamed" />
		</servers>
	</option>
</configuration>
```

The three values are reliability control, publisher incoming, and subscriber outgoing. A two-value configuration is `Incoming,Outgoing`; when Storage is available, Control is selected dynamically. Control starts only when the Server has an `IMessageStorage`. Port precedence is: explicit startup arguments, the named server's own `Port`, the collection-level `Servers.Port`, then dynamic selection. `*` explicitly requests dynamic ports.

For standalone applications, start the exchange directly:

```csharp
using var server = new ZeroQueueServer();
server.Storage = ResolveMessageStorage(); // Supplied by an independent storage plugin; only LeastOnce needs it.
await server.StartAsync(["--control:32100", "--incoming:32101", "--outgoing:32102"]);
```

### Client Connection

Define a `ZeroMQ` connection under `/Messaging/ConnectionSettings`:

```xml
<configuration>
	<option path="/Messaging">
		<connectionSettings default="ZeroMQ">
			<connectionSetting connectionSetting.name="ZeroMQ"
			                   driver="ZeroMQ"
			                   value="server=127.0.0.1;port=7969;group=Demo;client=MyApplication;" />
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

<a name="usage"></a>
## Usage

### Publish and Subscribe

Create a queue directly when the application does not use the Zongsoft plugin container:

```csharp
using System.Text;
using Zongsoft.Messaging;
using Zongsoft.Messaging.ZeroMQ;
using Zongsoft.Messaging.ZeroMQ.Configuration;

var settings = ZeroConnectionSettingsDriver.Instance.GetSettings(
	"ZeroMQ",
	"server=127.0.0.1;port=7969;group=Demo;client=Sample;");

using var queue = new ZeroQueue("ZeroMQ", settings);

var consumer = await queue.SubscribeAsync("orders/created", message =>
	Console.WriteLine(Encoding.UTF8.GetString(message.Data.Span)));

var identifier = await queue.ProduceAsync("orders/created", "Order #1001".AsMemory());
if(identifier == null)
	Console.WriteLine("No matching subscription was visible at send time; nothing was sent.");

await consumer.UnsubscribeAsync();
```

When hosted as a plugin, resolve the named queue from the `ZeroMQ` `IMessageQueueProvider` and use the same `ProduceAsync` and `SubscribeAsync` APIs.

Subscriptions use prefix matching. One `ZeroQueue` keeps one consumer for each logical topic; subscribing to the same topic again returns the existing consumer and does not replace its handler or options. With `Group=Demo`, the physical wire topic is `Demo:orders/created`, while handlers receive the logical `Message.Topic` value `orders/created`.

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

```csharp
var subscriptionOptions = new MessageSubscribeOptions(MessageReliability.LeastOnce);
var enqueueOptions = new MessageEnqueueOptions(MessageReliability.LeastOnce)
{
	Expiration = TimeSpan.FromMinutes(5),
};

server.Storage = ResolveMessageStorage(); // Supplied by an independent storage plugin.

var consumer = await queue.SubscribeAsync("orders/created", new ReliableOrderHandler(), subscriptionOptions);

var identifier = await queue.ProduceAsync("orders/created", payload, enqueueOptions);

sealed class ReliableOrderHandler : HandlerBase<Message>
{
	protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
	{
		await SaveOrderAsync(message.Data, cancellation);
		await message.AcknowledgeAsync(cancellation);
	}
}
```

The Broker accepts a publication only when an online matching subscription exists. No match returns `null` without writing Storage. With a match, the Broker persists Pending first and then returns the identifier. Delivery competes among online subscribers; any one acknowledgement removes Pending. Retries reuse `Message.Identifier` and may choose another consumer, so handlers must be idempotent.

Assign `ZeroQueueServer.Storage` only while the Server is stopped. With `Storage.Disposable=false`, the container or application owns it. With `true`, the Server disposes it when the Server itself is disposed, preferring `IAsyncDisposable`; ordinary Stop does not dispose Storage, so the Server can restart. A Broker without Storage still serves `MostOnce` Broadcast, but does not start Control and returns only `Incoming,Outgoing` in discovery `Ports`; `LeastOnce` operations then fail.

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

`ZeroRequester` and `ZeroResponder` adapt queue topics to the Zongsoft communication interfaces. A request is published to its URL topic, and responses use the `<url>/reply` topic by default:

```csharp
await using var requester = new ZeroRequester { Queue = queue };
var token = await requester.RequestAsync("services/ping", "Ping"u8.ToArray());

foreach(var response in token.GetResponses(TimeSpan.FromSeconds(3)))
	Console.WriteLine(Encoding.UTF8.GetString(response.Data.Span));
```

A responder subscribes to the URLs exposed by its registered handlers:

```csharp
var responder = new ZeroResponder { Queue = queue };
responder.Handlers.Add(new PingHandler());
await responder.StartAsync([]);
```

The handler receives an `IRequest` and can return data through the supplied `IResponder`. See [requester tests](test/ZeroRequesterTests.cs) and [responder tests](test/ZeroResponderTests.cs) for complete handler examples.

### Event Channel

`ZeroQueueEventChannel` connects an `EventExchanger` to queue topics under `Events/...`:

```csharp
await using var channel = new ZeroQueueEventChannel(queue);
await channel.OpenAsync(exchanger);
await channel.SendAsync(eventContext);
```

The plugin manifest registers this channel automatically for hosted applications. Request/response and event channels support `Group`; the prefix is applied only at the network boundary and adapters always use logical topics.

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

Message storage is an independent plugin concept, not part of the ZeroMQ driver. This package provides no default file store. Applications using `LeastOnce` must assign an independent `IMessageStorage` instance to each Broker Server. `Name` identifies the implementation, while `Settings` defines that instance's connection and data scope. Storage supports exact-topic reads and clears. An implementation must hold a message snapshot before `SetAsync` returns and provide the required restart durability.

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

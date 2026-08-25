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

The included `ZeroQueueServer` is a lightweight XPUB/XSUB exchange. Clients first query its discovery endpoint, then connect to the returned publisher and subscriber endpoints. The exchange is stateless and is intended for low-latency, transient message distribution rather than durable queueing.

<a name="features"></a>
## Features

- Implements the Zongsoft `IMessageQueue`, `IRequester`, `IResponder`, and `IEventChannel` abstractions;
- Supports multiple publishers and subscribers through an XPUB/XSUB exchange;
- Supports topic prefixes, optional message groups, instance filtering, and heartbeats;
- Supports Brotli payload compression above a configurable threshold;
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
| Discovery | `7969` | Clients request the two data endpoint ports. |
| Publisher ingress | `32101` | Application publishers connect here. |
| Subscriber egress | `32102` | Application subscribers connect here. |

`7969` is the built-in discovery-port default. The two data ports are configurable; when they are omitted, `ZeroQueueServer` binds random ports and returns them through discovery. Prefer fixed data ports in deployments so existing clients can reconnect to the same endpoints after an exchange restart.

The server binds TCP endpoints on all network interfaces and does not configure authentication or encryption. Restrict access at the host or network boundary, or add an authenticated transport before using it across an untrusted network.

<a name="configuration"></a>
## Configuration

### Server

The packaged daemon plugin starts `ZeroQueueServer` automatically. Configure its data endpoints under `/Messaging/ZeroMQ/Servers`:

```xml
<configuration>
	<option path="/Messaging/ZeroMQ">
		<servers port="32101,32102">
			<server server.name="unnamed" port="*" />
		</servers>
	</option>
</configuration>
```

The collection-level `port` is used by the default server or when no matching named server entry exists. A matching `<server>` entry supplies its own pair. The first number is publisher ingress and the second is subscriber egress; `*` selects random data ports.

For standalone applications, start the exchange directly:

```csharp
using var server = new ZeroQueueServer();
await server.StartAsync(["--incoming:32101", "--outgoing:32102"]);
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

The default filter excludes messages produced by the same queue instance. Use `Filter=*` to accept every instance, `Filter=.` (or `~`) to accept only the current instance, ordinary identifiers as an allow list, and `!identifier` entries as exclusions.

<a name="usage"></a>
## Usage

### Publish and Subscribe

Create a queue directly when the application does not use the Zongsoft plugin container:

```csharp
using System.Text;
using Zongsoft.Messaging.ZeroMQ;
using Zongsoft.Messaging.ZeroMQ.Configuration;

var settings = ZeroConnectionSettingsDriver.Instance.GetSettings(
	"ZeroMQ",
	"server=127.0.0.1;port=7969;group=Demo;client=Sample;");

using var queue = new ZeroQueue("ZeroMQ", settings);

var consumer = await queue.SubscribeAsync("orders/created", message =>
	Console.WriteLine(Encoding.UTF8.GetString(message.Data.Span)));

await queue.ProduceAsync("orders/created", "Order #1001".AsMemory());

await consumer.UnsubscribeAsync();
```

When hosted as a plugin, resolve the named queue from the `ZeroMQ` `IMessageQueueProvider` and use the same `ProduceAsync` and `SubscribeAsync` APIs.

Subscriptions use prefix matching. One `ZeroQueue` keeps one consumer for each logical topic; subscribing to the same topic again returns the existing consumer and does not replace its handler or options. With `Group=Demo`, the physical wire topic is `Demo:orders/created`, while handlers receive the logical `Message.Topic` value `orders/created`.

Each subscriber invokes its handler sequentially in receive order. When its bounded pending queue reaches capacity, that subscriber pauses Poller reads and resumes after the handler frees capacity; other sockets remain responsive.

### Compression

Set the `Compressive` property to the minimum payload size, in bytes, at which Brotli compression is enabled:

```csharp
var options = new MessageEnqueueOptions();
options.Properties["Compressive"] = 4 * 1024;

await queue.ProduceAsync("documents/updated", payload, options);
```

Compression is currently the only `MessageEnqueueOptions` behavior implemented by this adapter.

| Messaging option | Support |
| --- | --- |
| `Properties["Compressive"]` | Supported; enables Brotli above the specified byte threshold. |
| Tags | Not used by the ZeroMQ adapter. |
| Delay and expiration | Not implemented. |
| Priority | Not implemented. |
| Reliability | The transport remains transient, best-effort PUB/SUB. |
| Subscription reliability and fallback | Not implemented by the current handler dispatcher. |

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

This adapter follows ZeroMQ PUB/SUB behavior:

- Messages are transient and are not persisted by `ZeroQueueServer`;
- There is no broker acknowledgement, consumer acknowledgement, retry, deduplication, or replay;
- Messages may be dropped while peers connect or reconnect, when no matching subscription has propagated, or when a socket high-water mark is reached;
- `ProduceAsync` snapshots the caller's payload and completes after the Actor invokes the local Socket send; it does not mean that a subscriber received or handled the message;
- A queue snapshots its connection, ports, group, filter, timeout, and heartbeat settings at construction; mutating the original settings object does not reconfigure a running queue;
- `SubscribeAsync` synchronizes the subscriber connection, but does not establish an end-to-end delivery acknowledgement with publishers;
- Empty payloads should be avoided with the current release.

Use a durable broker or add an application-level acknowledgement protocol when loss is not acceptable.

<a name="samples"></a>
## Samples and Troubleshooting

The [.NET 10 samples](samples) contain an interactive exchange server and client. Start the server first, then run one client as a subscriber and another as a publisher. See the [sample guide](samples/README.md) for commands.

If messages are not received:

1. Verify that the discovery port and both returned data ports are reachable in the required directions;
2. Verify that publisher and subscriber use the same `Group` and compatible topic prefixes;
3. Check the `Filter` setting—self-produced messages are excluded by default;
4. Start subscriptions before publishing and allow for PUB/SUB subscription propagation;
5. Keep data ports fixed across server restarts, or recreate queues so they perform discovery again.

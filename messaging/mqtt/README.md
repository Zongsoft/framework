# Zongsoft.Messaging.Mqtt Message Queue Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Mqtt)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.Mqtt)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Samples

The `samples` directory contains two interactive .NET 10 applications:

- [Broker sample](samples/server/Program.cs) starts an MQTT Broker powered by `MqttQueueServer`.
- [Client sample](samples/client/Program.cs) connects through `MqttQueue` and can subscribe, unsubscribe, publish, and receive messages.

The client connects to `127.0.0.1:1883` by default. Each client process creates a unique ClientId, so multiple instances can be used together for end-to-end verification.

> The Broker sample does not configure authentication or TLS. Use it only in a trusted development or test environment.

### Prerequisites and build

Install the .NET 10 SDK. From the repository root, build the core library first and then the MQTT solution:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet build messaging/mqtt/Zongsoft.Messaging.Mqtt.slnx
```

### 1. Start the Broker

Open the first terminal and run:

```shell
dotnet run --project messaging/mqtt/samples/server/Zongsoft.Messaging.Mqtt.Samples.Server.csproj
```

The Broker starts automatically on port `1883`. Enter `info` at the interactive prompt to confirm its state and inspect connected channels and sessions.

| Command | Description |
| --- | --- |
| `info [--topic:<topic>]` | Show the Broker state, channels, sessions, and retained messages; optionally show one retained message and its content. |
| `stop` | Stop the Broker and release its listening port. |
| `start` | Start the Broker again on port `1883`. |

### 2. Start a subscriber

Open a second terminal and start the Client sample:

```shell
dotnet run --project messaging/mqtt/samples/client/Zongsoft.Messaging.Mqtt.Samples.Client.csproj
```

Subscribe to a test topic:

```text
subscribe samples/demo
```

The aliases `sub` and `unsub` can be used instead of `subscribe` and `unsubscribe`. MQTT wildcard filters are also supported, for example:

```text
subscribe samples/#
```

### 3. Publish and verify a message

Open a third terminal and start another Client sample with the same `dotnet run` command. Publish a message:

```text
produce --topic:samples/demo "Hello from the MQTT sample"
```

The alias `send` can be used instead of `produce`. The publishing client prints the MQTT packet identifier and elapsed time. The subscriber terminal should display output similar to:

```text
[Received]#1 Topic:samples/demo
[1]Hello from the MQTT sample
```

Return to the Broker terminal and enter `info`; it should report two channels, their associated sessions, and a summary of all retained messages. Each channel entry includes its remote endpoint, protocol version, connection time, packet and byte counters. Each session entry includes its lifecycle timestamps, expiry interval, and pending message count.

If a retained message has been published by an MQTT application or test tool, inspect it by topic:

```text
info --topic:samples/demo
```

The command displays the retained message topic, payload size, and UTF-8 content, or `N/A` when that topic has no retained message.

### Server status APIs

Applications can query the same information through the `MqttQueueServer` API:

```csharp
var channels = server.Channels;
var sessions = server.Sessions;
var retainedMessages = await server.GetRetainedMessagesAsync();
var retained = await server.GetRetainedMessageAsync("samples/demo");

foreach(var channel in channels)
	Console.WriteLine($"{channel.Identifier}: {channel.Address}");

foreach(var session in sessions)
	Console.WriteLine($"{session.Identifier}: {session.PendingApplicationMessagesCount}");

foreach(var message in retainedMessages)
	Console.WriteLine($"{message.Topic}: {message.Data.Length} bytes");

if(!retained.IsEmpty)
	Console.WriteLine(Encoding.UTF8.GetString(retained.Data));
```

`ChannelCollection` and `SessionCollection` are keyed by ClientId/SessionId. The Broker keeps both long-lived collections synchronized as clients connect or disconnect and sessions are created or deleted; callers do not need to refresh them. `GetRetainedMessagesAsync()` returns an empty array when the Broker is stopped or has no retained messages. Querying an empty topic, a stopped server, or a topic without a retained message through `GetRetainedMessageAsync()` returns `Message.Empty`.

### Batch and concurrency verification

Use the `round` option to publish the same payload repeatedly:

```text
produce --topic:samples/demo --round:1000 "Load test message"
```

The subscriber displays every received message with an incrementing counter. The publishing client reports the total elapsed time, which is useful for a quick throughput check.

### Reconnection verification

Keep both clients running and use the following sequence in the Broker terminal:

```text
stop
start
```

Wait for the client's reconnect interval, which defaults to two seconds, and publish another message. The subscriber should receive it without subscribing again because `MqttQueue` reconnects and restores active subscriptions automatically.

### Client commands

| Command | Description |
| --- | --- |
| `info` | Show the connection settings and active subscriptions. |
| `subscribe <topic> [...]` | Subscribe to one or more topics or wildcard filters. Alias: `sub`. |
| `unsubscribe <topic> [...]` | Remove one or more subscriptions. Alias: `unsub`. |
| `produce --topic:<topic> [--round:<count>] <message> [...]` | Publish one or more messages. Alias: `send`. |
| `reset` | Reset the displayed receive counter. |
| `close` | Dispose the MQTT queue; restart the Client sample to continue testing. |

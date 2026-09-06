# Zongsoft.Messaging.ZeroMQ Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [server](server) | Runs the ZeroMQ queue server with separate incoming and outgoing endpoints. |
| [client](client) | Demonstrates topic subscription, message publishing, and received-message handling through `ZeroQueue`. |

Both projects target .NET 10. The server listens for endpoint discovery on port `7969` and binds incoming port `32101` plus outgoing port `32102`. The client connects to `127.0.0.1:7969` and obtains the message endpoints from the server.

## Run

Start the server from the repository root:

```shell
dotnet run --project messaging/zero/samples/server/Zongsoft.Messaging.ZeroMQ.Samples.Server.csproj
```

Then start the client in another terminal:

```shell
dotnet run --project messaging/zero/samples/client/Zongsoft.Messaging.ZeroMQ.Samples.Client.csproj
```

## Server Commands

The server starts automatically with `--incoming:32101 --outgoing:32102`. Use the following commands to inspect or restart it:

```text
info
stop
start --incoming:32101 --outgoing:32102
```

`info` prints the worker state and discovery port. Options supplied to `start` are forwarded to `ZeroQueueServer`, allowing the incoming and outgoing endpoints to be changed; clients discover those endpoint changes automatically through port `7969`.

## Client Subscriptions

Subscribe to one or more topics. `sub` is an alias for `subscribe`:

```text
subscribe demo notifications
sub telemetry
```

Remove subscriptions with `unsubscribe` or `unsub`:

```text
unsubscribe notifications telemetry
```

Every received message is printed with a sequence number, topic, and UTF-8 payload.

## Client Publishing

The required `--topic` option selects the destination. Every positional argument becomes a separate message:

```text
produce --topic:demo hello
produce --topic:demo first second third
```

Use `--round:<count>` to repeat all messages. `send` is an alias for `produce`:

```text
produce --topic:demo --round:3 "Hello ZeroMQ"
send --topic:telemetry --round:2 value-1 value-2
```

Use `info` in the client to display its settings and active subscriptions, `reset` to clear the received-message counter, and `close` to dispose the queue.

## Suggested Scenario

1. Start the server and two client processes. The unmodified [client/Program.cs](client/Program.cs) uses `Group=Demo` and the default filter, which excludes messages produced by the same queue instance. A single client cannot demonstrate loopback reception with these settings.
2. In client A, subscribe and wait for the command to complete:

```text
subscribe demo
```

3. In client B, publish:

```text
produce --topic:demo --round:3 "Hello ZeroMQ"
```

4. Observe received output in client A before inspecting and removing its subscription:

```text
info
unsubscribe demo
```

5. Three received messages are expected with a ready matching route and no other producers; `MostOnce` is not an acknowledgment or replay guarantee. A null publication identifier means there was no matching route at publication time. See the [delivery and filter rules](../README.md) before interpreting the output.
6. Run `stop`, `info`, and `start --incoming:32101 --outgoing:32102` in the server to exercise its lifecycle.

## Safety and Cleanup

🚨 The [sample server](server/Program.cs) has no message storage attached and does not establish authentication or encryption. Its listeners bind network interfaces; isolate the ports with your local environment/firewall and never expose them publicly. This sample demonstrates broadcast delivery, not durable recovery.

Unsubscribe in the clients, then run `close` and `exit` (confirm the prompt). Run `stop` and `exit` in the server. This sample creates no persistent message store to erase. For plugin deployment and public queue-provider use, follow the [library guide](../README.md); concrete queue construction here belongs to a standalone diagnostic tool.

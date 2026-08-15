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

1. Start the server and then the client.
2. Run the following commands in the client:

```text
subscribe demo
produce --topic:demo --round:3 "Hello ZeroMQ"
info
unsubscribe demo
```

3. Confirm that three messages are received and that `demo` disappears from the subscription list.
4. Run `stop`, `info`, and `start --incoming:32101 --outgoing:32102` in the server to exercise its lifecycle.

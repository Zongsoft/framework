# Zongsoft.Messaging.Mqtt Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [server](server) | Runs an MQTT broker and displays channels, sessions, retained messages, and received application messages. |
| [client](client) | Connects to an MQTT broker and demonstrates publishing, subscriptions, acknowledgment, and queue inspection. |

Both projects target .NET 10. The client defaults to `127.0.0.1:1883`, which matches the server's default listener.

## Run

Start the broker from the repository root:

```shell
dotnet run --project messaging/mqtt/samples/server/Zongsoft.Messaging.Mqtt.Samples.Server.csproj
```

Then start the client in another terminal:

```shell
dotnet run --project messaging/mqtt/samples/client/Zongsoft.Messaging.Mqtt.Samples.Client.csproj
```

## Server Commands

The server starts automatically. Use these commands in its terminal:

```text
info
info --topic:demo
stop
start
```

`info` displays the worker state, listening port, connected channels, sessions, and all retained messages. With `--topic:<name>`, it displays only the retained message for that topic. `stop` and `start` let you exercise broker lifecycle behavior without restarting the process.

## Client Subscriptions

Subscribe to one or more topics. The short alias is `sub`:

```text
subscribe demo notifications
sub telemetry
```

Remove subscriptions with `unsubscribe` or `unsub`:

```text
unsubscribe notifications telemetry
```

Every received message is printed with the MQTT client identity, topic, and UTF-8 payload.

## Client Publishing

The required `--topic` option selects the destination. Every positional argument becomes a separate message:

```text
produce --topic:demo hello
produce --topic:demo first second third
```

Repeat all messages with `--round:<count>`. `send` is an alias for `produce`:

```text
produce --topic:demo --round:3 "Hello MQTT"
send --topic:telemetry --round:2 value-1 value-2
```

Use `info` in the client to display the queue and active subscriptions, `reset` to clear the received-message counter, and `close` to dispose the client queue.

## Suggested Scenario

1. Start the server, then start the client.
2. Run these commands in the client:

```text
subscribe demo
produce --topic:demo --round:3 "Hello MQTT"
info
```

3. Run `info` in the server and confirm that the client channel and session are listed.
4. Run `stop` and `start` in the server to observe the client connection lifecycle, then run `unsubscribe demo` in the client.

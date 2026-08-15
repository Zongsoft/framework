# Zongsoft.Externals.Redis Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [messaging](messaging) | Demonstrates Redis-backed message publishing, tagged subscriptions, acknowledgment, and queue inspection. |
| [distributedlock](distributedlock) | Validates mutual exclusion and expiry behavior with multiple processes competing for a Redis distributed lock. |

All projects target .NET 10 and require a reachable Redis server. Review the connection strings in each sample before running them; the messaging sample defaults to `127.0.0.1:6379` with password `xxxxxx`.

## Messaging Sample

Run the interactive messaging client from the repository root:

```shell
dotnet run --project externals/redis/samples/messaging/Zongsoft.Externals.Redis.Messaging.Samples.csproj
```

### Subscribe and Unsubscribe

Subscribe to one or more topics. `sub` is an alias for `subscribe`:

```text
subscribe orders invoices
subscribe --tags:urgent notifications
sub --tags:region-a telemetry
```

The optional `--tags` value is passed to the Redis subscriber for tag-based filtering. Remove subscriptions with `unsubscribe` or `unsub`:

```text
unsubscribe invoices notifications
```

### Produce Messages

The required `--topic` option selects the destination. Each positional argument is published as a separate UTF-8 message:

```text
produce --topic:orders "order #1001"
produce --topic:orders first second third
produce --topic:notifications --tags:urgent "service unavailable"
```

Use `--round:<count>` to publish every argument repeatedly. `send` is an alias for `produce`:

```text
produce --topic:orders --round:3 hello
send --topic:telemetry --tags:region-a --round:2 value-1 value-2
```

The sample prefixes each payload with its round number, prints the returned message identifier and elapsed time, and acknowledges messages after the handler displays them.

### Inspect and Reset

```text
info
reset
close
```

`info` displays the queue and every active subscription, including its tags. `reset` clears the received-message counter. `close` disposes the queue; restart the process before issuing more messaging commands.

### Suggested Scenario

Run the following commands with matching tags:

```text
subscribe --tags:urgent alerts
produce --topic:alerts --tags:urgent --round:3 "High temperature"
info
unsubscribe alerts
```

You should observe three received messages and an `alerts` subscription tagged `urgent` before it is removed.

## Distributed Lock Sample

The distributed-lock sample contains cooperating master and slaver processes. See its [complete instructions](distributedlock/README.md) for build commands, automatic and manual scenarios, and connection overrides.

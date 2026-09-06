# Zongsoft.Externals.Redis Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [messaging](messaging) | Demonstrates Redis-backed message publishing, tagged subscriptions, acknowledgment, and queue inspection. |
| [distributedcache](distributedcache) | Demonstrates Redis distributed cache operations, expiry management, and keyspace change notifications. |
| [distributedlock](distributedlock) | Validates mutual exclusion, expiry behavior, automatic renewal, and fencing tokens with multiple processes competing for a Redis distributed lock. |

All projects target .NET 10 and require a reachable Redis server. Review the connection strings in each sample before running them; the messaging sample defaults to `127.0.0.1:6379` with password `xxxxxx`.

## Messaging Sample

Run the interactive messaging client from the repository root:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project externals/redis/samples/messaging/Zongsoft.Externals.Redis.Messaging.Samples.csproj
```

The Debug project references the locally built Core DLL. [messaging/Program.cs](messaging/Program.cs) owns a concrete diagnostic queue; application consumers should use the [plugin and public provider workflow](../README.md).

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

Run the following commands individually with matching tags. Wait for subscription completion before publishing, and for received output before unsubscribing:

```text
subscribe --tags:urgent alerts
produce --topic:alerts --tags:urgent --round:3 "High temperature"
info
unsubscribe alerts
```

You should observe three received messages and an `alerts` subscription tagged `urgent` before it is removed.

💡 All sample processes use the same consumer group by default. Multiple clients therefore compete for stream entries rather than each receiving all three messages. Use one consuming client and a unique test topic for this observation; the displayed count is not an exactly-once delivery guarantee.

### Safety and Cleanup

🚨 Use only a dedicated local Redis database and fake messages. Streams, groups, and pending entries can remain after client shutdown; acknowledgment does not imply deletion of the stream. After canceling this run's subscriptions, execute `close`, then `exit` and confirm. If persistent cleanup is required, inspect the actual stream keys and remove only resources created for this test using Redis management tools; never use a shared-database flush.

## Distributed Cache Sample

The distributed-cache sample is an interactive client for key/value operations, expiry management, and change notifications. See its [complete instructions](distributedcache/README.md) for the command reference and a two-terminal notification scenario.

## Distributed Lock Sample

The distributed-lock sample contains cooperating master and slaver processes. See its [complete instructions](distributedlock/README.md) for build commands, automatic and manual scenarios, and connection overrides.

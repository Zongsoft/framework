# Redis Distributed Cache

This sample demonstrates the Redis distributed cache implementation in `RedisService.DistributedCache.cs`. It is an interactive terminal client that exposes cache operations as commands, following the style of the [messaging](../messaging) sample.

The sample covers:

- Key/value operations: set with optional expiry and requisite, get, exists, remove.
- Expiry management: query the remaining lifetime, extend or make an entry permanent.
- Inspection: total entry count and pattern-based key search.
- Cache notifications: subscribe to keyspace notifications and print every received `DistributedCacheNotification`.

## Requirements

- A reachable Redis server. The default endpoint is `127.0.0.1:6379` with password `xxxxxx`; edit the connection string in `Program.cs` if needed.
- Redis keyspace notifications must include `K` and `A` events for the subscription commands (recommended setting: `notify-keyspace-events KA`). Set/get/remove commands work without it.

## Build and Run

```pwsh
dotnet run --project externals\redis\samples\distributedcache\Zongsoft.Externals.Redis.DistributedCache.Samples.csproj -c Debug
```

All cache keys are prefixed with the `DistributedCache` namespace, so entries written by one client are visible to every other client using the same namespace — start several terminals to observe notifications across processes.

## Commands

### Set and Get

`set` stores a value; the `--expiry` option sets a lifetime and the `--requisite` option constrains the write:

```text
set --key:greeting hello
set --key:token --expiry:30s "a temporary value"
set --key:config --requisite:notexists "created only if absent"
set --key:config --requisite:exists "updated only if present"
```

`get` prints the value and the remaining lifetime:

```text
get greeting
get token
```

`exists` and `remove` (alias `del`) are self-explanatory:

```text
exists greeting
remove greeting
```

### Expiry

`expiry` without `--expiry` prints the remaining lifetime; with `--expiry` it sets it (`0` makes the entry permanent):

```text
expiry token
expiry greeting --expiry:1h
expiry greeting --expiry:0
```

### Inspect

```text
count
find gre*
info
```

`count` reports the number of entries in the namespace, `find` lists keys matching a pattern (default `*`), and `info` shows the service name, namespace, database, entry count, and subscription state. `purge` removes every entry in the namespace:

```text
purge
```

### Subscribe to Notifications

`subscribe` (alias `sub`) registers a notification handler. The optional `--prefix` filters logical keys and `--kind` selects `updated`, `removed`, `expired`, `evicted`, or `all` (default):

```text
subscribe
subscribe --prefix:orders:
subscribe --kind:updated --prefix:telemetry:
```

After subscribing, every matching change — from this process or any other process sharing the namespace — is printed as `[Received] Kind:... Key:...`. `unsubscribe` (alias `unsub`) cancels the subscription, and `reset` clears the received-notification counter.

## Suggested Scenario

Open two terminals. In the first terminal subscribe, then use the second terminal to change entries and watch the notifications:

```text
subscribe --kind:updated
```

```text
set --key:orders:1001 "in progress"
set --key:orders:1002 --expiry:10s "completed"
remove orders:1001
```

You should observe `Updated` notifications for the two `set` commands and a `Removed` notification for the `remove` command in the first terminal. Use `info` to confirm the subscription state, then `unsubscribe` to stop receiving notifications.

# Zongsoft.Externals.Redis Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**R**edis](https://github.com/Zongsoft/framework/tree/main/externals/redis) integrates [Redis](https://redis.io/) with the infrastructure abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. It is built on [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) and can be used as a plugin or referenced directly by an application.

## Features

- Registers named Redis services from `Redis` connection settings.
- Provides key/value, dictionary, hash-set, sequence, and distributed-lock operations.
- Implements the framework's message queue and subscription abstractions with Redis.
- Provides a reliable-message storage factory at `/Workspace/Messaging/Storages/Redis`.
- Supplies a Microsoft configuration provider and distributed-cache integration.
- Adds Redis inspection, mutation, counter, search, and lock commands to the Zongsoft command tree.

Load `Zongsoft.Externals.Redis.plugin` and configure `/Externals/Redis/ConnectionSettings`. Messaging connections can be configured separately under `/Messaging/ConnectionSettings`; both use the `Redis` driver. See the [distributed-lock sample](samples/distributedlock), the [distributed-cache sample](samples/distributedcache), the [messaging sample](samples/messaging), and the [tests](test) for working examples.

For reliable Broker storage, name the Redis connection exactly after the Broker and inject the factory path. The daemon-created ZeroMQ Broker is named `QueueServer`:

Set a stable storage identifier before the process starts:

```powershell
$env:ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER = "broker-storage-01"
```

```xml
<option path="/Externals/Redis">
	<connectionSettings>
		<connectionSetting connectionSetting.name="QueueServer" driver="Redis"
		                   value="server=127.0.0.1:6379;password=;" />
	</connectionSettings>
</option>
<extension path="/Workbench/Messaging/Zero">
	<QueueServer.Storages>{path:/Workspace/Messaging/Storages/Redis}</QueueServer.Storages>
</extension>
```

The factory never falls back to a default connection. It freezes `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` on first use and falls back to `Environment.MachineName` when the variable is empty. Storage keys use `Zongsoft.Messaging.Storage:{ConnectionSettings.Name}:{StorageIdentifier}` as their prefix; overlong partitions use a stable SHA-256 form.

When upgrading from the former `nodeId` option, set `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` to the same value before starting the Broker. The partition text remains compatible when the value is unchanged; omitting it may select the machine-name partition and leave previous reliable messages under the old prefix.

Redis streams retain up to `100000` messages by default and use approximate trimming. Configure `MaximumLength` and `UseApproximateMaximumLength` in the messaging connection settings to change this behavior; use a negative `MaximumLength` to disable trimming. Dead-letter transfer atomically appends and acknowledges through a same-slot Lua script.

Cache notification subscriptions require Redis keyspace notifications (recommended setting: `notify-keyspace-events KA`). Notifications have Redis Pub/Sub at-most-once semantics and are not replayed after a disconnection.

Caches, queues, configuration providers, and the Microsoft distributed cache share one `ConnectionMultiplexer` when their connection options are equivalent; independent leases control ownership. Use `RedisService.WithDatabase()` and `WithNamespace()` for immutable scopes. The legacy `Use()` and `Namespace` members may only change a service before its first operation.

Notifications use one Redis subscription per scope and one bounded local queue per consumer. The default capacity is `1024`, with drop-oldest overflow behavior. The configuration provider keeps a detached local snapshot and reloads it after matching key notifications.

Redis locks expose monotonically increasing fencing tokens and explicit renewal. Automatic renewal is disabled by default and is enabled only through `DistributedLockOptions.RenewalInterval`; an uncertain connection or failed renewal is treated as loss of ownership, so protected writes should validate fencing tokens.

`RedisServiceInfo.Capabilities` and `RedisQueue.Capabilities` expose the conservative intersection across primary nodes: `XAUTOCLAIM` at Redis 6.2, `XACKDEL`/group-aware trimming at 8.2, and Stream IDMP at 8.6. Older servers retain the existing fallback behavior. The `Zongsoft.Externals.Redis` diagnostics source provides both `ActivitySource` and `Meter` without requiring an additional telemetry package.

## Getting Started through the Cache Contract

Business modules only reference Core's [IDistributedCache](../../Zongsoft.Core/src/Caching/IDistributedCache.cs); the deployment composition selects the Redis plugin. RedisService construction is not the default business entry point.

This host-option adaptation uses the `Redis` connection name from the [actual distributed-cache sample](samples/distributedcache/Program.cs). Supply your isolated test endpoint and credentials through environment-specific configuration. It does not add an Orders business module or a custom cache-selection key:

```xml
<options>
	<option path="/Externals/Redis">
		<connectionSettings>
			<connectionSetting connectionSetting.name="Redis" driver="Redis"
			                   value="server=REPLACE_WITH_HOST:REPLACE_WITH_PORT;password=REPLACE_WITH_PASSWORD;database=15" />
		</connectionSettings>
	</option>
</options>
```

This is a host-based adaptation of the sample’s cache read/write operations, using only the shared contract. The sample itself is a standalone process that owns its RedisService. After host initialization, run the fragment in a service/command:

```csharp
using Zongsoft.Caching;
using Zongsoft.Services;

var application = ApplicationContext.Current;
var qualifiedName = "Redis@Redis";
var cache = application.Services.Locate<IDistributedCache>(qualifiedName)
	?? throw new InvalidOperationException("The configured cache is unavailable.");
var key = "Zongsoft.Externals.Redis.Samples:" + Guid.NewGuid().ToString("N");

try
{
	await cache.SetValueAsync(key, "hello", TimeSpan.FromMinutes(1));
	Console.WriteLine(await cache.GetValueAsync<string>(key));
}
finally
{
	await cache.RemoveAsync(key);
}
```

The expected output is `hello`. Only this call's generated key is removed; the example does not clear the database. Module code can use its own `Module.Current.Services`. Do not dispose the shared cache per operation.

### Provider Name versus Connection Name

In `Redis@Redis`, `Redis` is the registered provider alias and `Redis` is the connection name passed to its `GetService(name)`; Redis is not a module container here. You can also resolve `Zongsoft.Services.IServiceProvider<IDistributedCache>` and call `GetService("Redis")`, but explicitly select the provider when several coexist instead of relying on registration order.

[RedisServiceProvider](src/RedisServiceProvider.cs) reuses services by name. Ordinary cache/sequence/lock lookup tries the default connection when a named connection is missing. **Reliable message storage factories instead require an exact name.** A misspelled connection can therefore fall back unexpectedly. Check selected settings during startup without printing complete connection strings.

### Deployment Versions and First-Connection Troubleshooting

🚨 The current [deployment manifest](src/Zongsoft.Externals.Redis.deploy) deploys StackExchange.Redis before the Microsoft cache adapter. The latter's transitive dependency can overwrite the DLL with an older version. In local .NET 10 verification, 2.7.27 overwrote 3.1.31 and first use failed with a missing `ConfigurationOptions.get_SentinelUser` method. Appearing in `plugin.list` does not prove that the first connection will succeed.

For the current source version, after plugin deployment and **with the host stopped**, use a separate supplemental manifest to deploy the required version into the same plugin directory:

```ini
[plugins zongsoft externals redis]
nuget:StackExchange.Redis@3.1.31
```

```shell
dotnet deploy redis-runtime.deploy --destination:./out --framework:net10.0 --overwrite:alway
```

Save the INI above as `redis-runtime.deploy`. This command overwrites files, so target a verified test deployment. The [project file](src/Zongsoft.Externals.Redis.csproj) owns the required version; do not perpetually reuse an old workaround. Check the final DLL version, restart, and verify contract-level reads and writes.

For connection failures, check plugin dependency versions, named settings and driver, Windows/container addressing, port and credentials, then database permissions. A Windows host uses `127.0.0.1` with the published port; `localhost` inside a container refers to that container, not Windows.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest registers the Redis service provider, settings driver, commands and message-storage factory. Configure named connections and resolve cache/sequence/lock contracts through the provider; applications should not recreate the shared connection per operation.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Redis` | [Zongsoft.Externals.Redis.plugin](src/Zongsoft.Externals.Redis.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Redis.deploy](src/Zongsoft.Externals.Redis.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals redis]
nuget:Zongsoft.Externals.Redis
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Redis.plugin`, `Zongsoft.Externals.Redis.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

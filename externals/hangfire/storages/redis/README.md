# Zongsoft.Externals.Hangfire.Storages.Redis Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Storages.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Storages.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Hangfire.Storages.Redis` provides Redis-backed persistent storage for the [Zongsoft Hangfire integration](../..). It adapts `Hangfire.Redis.StackExchange` to Zongsoft connection settings and registers the resulting `RedisStorage` as Hangfire's `JobStorage`.

## Configuration

Load `Zongsoft.Externals.Hangfire.Storages.Redis.plugin` together with the core Hangfire and Redis plugins. The adapter reads `/Externals/Redis/ConnectionSettings`, preferring a setting named `Hangfire` whose driver is `Redis`; otherwise it falls back to the default or first Redis setting.

Configure and verify the Redis connection before starting the Hangfire server. All Hangfire jobs, states, queues, and server metadata are persisted through that connection.


## Installation and Resolution

```shell
dotnet add package Zongsoft.Externals.Hangfire.Storages.Redis
```

The registered `RedisStorage` lazily creates a StackExchange.Redis connection on first use. Resolution order is: exact `Hangfire` setting with driver `Redis`, then a default Redis setting, then the first Redis setting. If none is usable, startup will fail when storage is first accessed.

> 💡 Use a dedicated Redis database/instance and explicit `Hangfire` connection name so cache eviction or another subsystem cannot silently affect background jobs.

🚨 Redis is the system of record for pending jobs. Configure persistence, backups, memory policy, TLS/authentication and high availability; never use an eviction policy that can discard Hangfire keys. Connection creation is lazy, so include an active startup health check.

Validate enqueue, processing, retry, scheduled jobs, server restart and Redis failover against a disposable instance before production.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../../../Zongsoft.Plugins/README.md).

Keep both Hangfire and the framework Redis plugin in the deployment. Storage selection uses the framework's Redis configuration; verify the selected logical database and dedicated key space before any jobs run.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Hangfire.Storages.Redis` | [Zongsoft.Externals.Hangfire.Storages.Redis.plugin](Zongsoft.Externals.Hangfire.Storages.Redis.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Hangfire.Storages.Redis.deploy](Zongsoft.Externals.Hangfire.Storages.Redis.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals hangfire]
nuget:Zongsoft.Externals.Hangfire

[plugins zongsoft externals redis]
nuget:Zongsoft.Externals.Redis

[plugins zongsoft externals hangfire storages redis]
nuget:Zongsoft.Externals.Hangfire.Storages.Redis
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Hangfire.Storages.Redis.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

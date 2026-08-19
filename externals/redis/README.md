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
- Supplies a Microsoft configuration provider and distributed-cache integration.
- Adds Redis inspection, mutation, counter, search, and lock commands to the Zongsoft command tree.

Load `Zongsoft.Externals.Redis.plugin` and configure `/Externals/Redis/ConnectionSettings`. Messaging connections can be configured separately under `/Messaging/ConnectionSettings`; both use the `Redis` driver. See the [distributed-lock sample](samples/distributedlock), the [distributed-cache sample](samples/distributedcache), the [messaging sample](samples/messaging), and the [tests](test) for working examples.

Redis streams retain up to `100000` messages by default and use approximate trimming. Configure `MaximumLength` and `UseApproximateMaximumLength` in the messaging connection settings to change this behavior; use a negative `MaximumLength` to disable trimming. Dead-letter transfer atomically appends and acknowledges through a same-slot Lua script.

Cache notification subscriptions require Redis keyspace notifications (recommended setting: `notify-keyspace-events KA`). Notifications have Redis Pub/Sub at-most-once semantics and are not replayed after a disconnection.

Caches, queues, configuration providers, and the Microsoft distributed cache share one `ConnectionMultiplexer` when their connection options are equivalent; independent leases control ownership. Use `RedisService.WithDatabase()` and `WithNamespace()` for immutable scopes. The legacy `Use()` and `Namespace` members may only change a service before its first operation.

Notifications use one Redis subscription per scope and one bounded local queue per consumer. The default capacity is `1024`, with drop-oldest overflow behavior. The configuration provider keeps a detached local snapshot and reloads it after matching key notifications.

Redis locks expose monotonically increasing fencing tokens and explicit renewal. Automatic renewal is disabled by default and is enabled only through `DistributedLockOptions.RenewalInterval`; an uncertain connection or failed renewal is treated as loss of ownership, so protected writes should validate fencing tokens.

`RedisServiceInfo.Capabilities` and `RedisQueue.Capabilities` expose the conservative intersection across primary nodes: `XAUTOCLAIM` at Redis 6.2, `XACKDEL`/group-aware trimming at 8.2, and Stream IDMP at 8.6. Older servers retain the existing fallback behavior. The `Zongsoft.Externals.Redis` diagnostics source provides both `ActivitySource` and `Meter` without requiring an additional telemetry package.

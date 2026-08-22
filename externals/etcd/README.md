# Zongsoft.Externals.Etcd Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Etcd)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Etcd)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Etcd` integrates [etcd](https://etcd.io/) with Zongsoft infrastructure abstractions. It can be loaded as a Zongsoft plugin or referenced directly by an application.

## Features

- Named etcd services and namespace-scoped UTF-8 key/value operations.
- Atomic integer and floating-point sequences through `ISequence` and `ISequenceBase`.
- Lease-based distributed locks with ownership tokens, manual or automatic renewal, and monotonically increasing fencing tokens.
- Etcd command-tree operations for get, set, find, count, remove, sequences, and locks.

## Connection settings

Load `Zongsoft.Externals.Etcd.plugin` and configure `/Externals/Etcd/ConnectionSettings`:

```xml
<connectionSettings>
	<connectionSetting connectionSetting.name="local" driver="etcd"
	                   value="server=127.0.0.1;port=2379;timeout=10s" />
</connectionSettings>
```

`server` also accepts a comma-separated endpoint list. `username` and `password` enable etcd authentication. Set `Namespace` before the first operation to isolate logical keys; it becomes immutable after activation.

## Direct usage

```csharp
using Zongsoft.Externals.Etcd;
using Zongsoft.Services.Distributing;

using var etcd = new EtcdService("local", "server=127.0.0.1;port=2379")
{
	Namespace = "orders"
};

await etcd.SetValueAsync("status", "ready");
var orderNumber = await etcd.IncreaseAsync("number", seed: 1000);

var options = new DistributedLockOptions(TimeSpan.FromSeconds(10))
{
	RenewalInterval = TimeSpan.FromSeconds(3)
};

await using var locker = await etcd.AcquireAsync("writer", options);
await locker.EnterAsync();
Console.WriteLine($"Fence: {locker.FencingToken}");
```

`AcquireAsync` is non-blocking and may return an unheld lock. Call `EnterAsync` when the caller should wait until ownership is obtained. Automatic renewal is disabled unless `RenewalInterval` is set. Protected storage should reject stale fencing tokens.

Etcd leases have whole-second granularity; positive sub-second expirations are rounded up to one second.

## Local etcd and samples

The Podman manifest is `D:\Zongsoft\hosting\zongsoft.pod-etcd.yaml`. It is also available as the `etcd` choice in the hosting Pod start/stop scripts.

- [Sequence sample](samples/sequence)
- [Distributed-lock sample](samples/distributedlock)

## References

- [dotnet-etcd documentation](https://github.com/shubhamranjan/dotnet-etcd/tree/main/docs)
- [etcd documentation](https://etcd.io/docs/)

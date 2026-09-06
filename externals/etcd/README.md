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
<options>
	<option path="/Externals/Etcd">
		<connectionSettings>
			<connectionSetting connectionSetting.name="local" driver="etcd"
			                   value="server=127.0.0.1;port=2379;timeout=10s" />
		</connectionSettings>
	</option>
</options>
```

`server` also accepts a comma-separated endpoint list. `username` and `password` enable etcd authentication. Set `Namespace` before the first operation to isolate logical keys; it becomes immutable after activation.

## Use Through the Plugin Service Container

Business modules consuming sequences or locks only need `Zongsoft.Core`. The following adapts the [real sequence sample](samples/sequence/Program.cs) to a bounded host call with a unique expiring key. Run it in a host command or application service after the Etcd plugin and configuration above are loaded. It assumes this container has only one sequence provider:

```csharp
using Zongsoft.Common;
using Zongsoft.Services;

var provider = ApplicationContext.Current.Services
	.ResolveRequired<Zongsoft.Services.IServiceProvider<ISequence>>();
var sequence = provider.GetService("local")
	?? throw new InvalidOperationException("Sequence service not found.");

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var key = $"docs:sequence:{Guid.NewGuid():N}";
var value = await sequence.IncreaseAsync(key, seed: 1000,
	expiry: TimeSpan.FromMinutes(1), cancellation: cancellation.Token);
Console.WriteLine(value);
```

A new key returns `1001` initially and expires after about one minute. The name selects the `local` connection setting, not a plugin. Do not dispose the container-owned provider or service. Inside a module, use `Module.Current.Services` where appropriate.

💡 The current Etcd provider has no service alias or name matcher for `Etcd`. Do not turn Redis's `local@Redis` syntax into an assumed `local@Etcd`. When several `IServiceProvider<ISequence>` implementations share a container, explicitly select/inject the intended provider in the application composition layer instead of relying on enumeration order.

Sequences and locks solve different problems. A sequence allocates values atomically but does not ensure business-transaction commits or gap-free numbering. A lock lease limits ownership time, but a paused or disconnected process can still execute stale code. The protected writer must therefore check a monotonic [fencing token](../../Zongsoft.Core/src/Services/Distributing/IDistributedLock.cs). Acquiring a lock without resource-side token validation cannot prevent stale-owner writes.

🚨 A connection name selects a service instance; it does not automatically prefix keys. Public-interface callers should use an agreed business key prefix. If `EtcdService.Namespace` is needed, configure it centrally before the first operation. Do not cast to the implementation and mutate shared configuration on each business call.

## Direct Use in Standalone Tools

The following low-level example owns its client lifetime; it is not the default pattern for collaboration between modules. Basic KV methods are Etcd-specific. Shared sequence and lock consumers should follow the interface-based approach above.

This is the actual [sequence sample](samples/sequence/Program.cs). Its `orders` and `score` keys are sample inputs, not an implemented order-processing service:

```csharp
using Zongsoft.Externals.Etcd;

var connectionString = args.Length > 0 ? args[0] : "server=127.0.0.1;port=2379";
using var sequence = new EtcdService("sample", connectionString) { Namespace = "samples:sequence" };

var number = await sequence.IncreaseAsync("orders", seed: 1000);
var fraction = await sequence.IncreaseAsync("score", 0.25, 1.5);
```

For lock acquisition and renewal, see the separate [distributed-lock sample](samples/distributedlock/master/Program.cs).


`AcquireAsync` is non-blocking and may return an unheld lock. Call `EnterAsync` when the caller should wait until ownership is obtained. Automatic renewal is disabled unless `RenewalInterval` is set. Protected storage should reject stale fencing tokens.

Etcd leases have whole-second granularity; positive sub-second expirations are rounded up to one second.

## Local etcd and samples

The Podman manifest is `D:\Zongsoft\hosting\zongsoft.pod-etcd.yaml`. It is also available as the `etcd` choice in the hosting Pod start/stop scripts.

- [Sequence sample](samples/sequence)
- [Distributed-lock sample](samples/distributedlock)

## References

- [dotnet-etcd documentation](https://github.com/shubhamranjan/dotnet-etcd/tree/main/docs)
- [etcd documentation](https://etcd.io/docs/)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest registers the Etcd provider, settings driver and command group. Configure named settings, then resolve the provider for sequence or lock contracts. Namespace and lease options remain part of the application contract.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Etcd` | [Zongsoft.Externals.Etcd.plugin](src/Zongsoft.Externals.Etcd.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Etcd.deploy](src/Zongsoft.Externals.Etcd.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals etcd]
nuget:Zongsoft.Externals.Etcd
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Etcd.option`, `Zongsoft.Externals.Etcd.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

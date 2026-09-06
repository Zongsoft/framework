# The Zongsoft.Hardwares Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Hardwares)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Hardwares)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**H**ardwares](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Hardwares) provides cross-platform functionality for retrieving hardware information.

It implements the hardware contracts from `Zongsoft.Core` for Windows, Linux, and macOS, and augments platform results with network interfaces. The result is a normalized hierarchy that applications can inspect or combine into a `HardwareProfile` identifier.

## What Hardware Identity Means

Hardware inventory is observational, not an attested identity. Firmware, virtualization, permissions, containers, hot-plug devices, operating-system updates, and vendor reporting can all change or hide values. A `HardwareProfile` provides a convenient normalized fingerprint; it is not a cryptographic proof and must not be the sole authentication factor.

## Installation and Collection

Complete [plugin integration](#plugin-based-integration) first. Run this code in an application service/command inside an initialized host. The consumer references only the Core contract, without referencing or constructing the concrete collector. Use your application-defined `Module.Current.Services` when a module-specific implementation is required.

```shell
dotnet add package Zongsoft.Core
```

```csharp
using Zongsoft.Services;
using Zongsoft.IO.Hardwares;

var collector = ApplicationContext.Current.Services.ResolveRequired<IHardwareCollector>();
var devices = collector.Collect();
var profile = new HardwareProfile(devices);

Console.WriteLine(profile.Identifier);
foreach(var device in profile)
	Console.WriteLine($"{device.Name}: {device.Code}");
```

`IHardwareCollector` also makes test substitution possible. `CollectAsync(cancellationToken)` exposes the same platform collection as an asynchronous stream and checks cancellation between yielded entries. The profile is a caller-owned value object; the collector is a shared host service.

## Platform Behavior

The collector dispatches to a platform gatherer on Windows, Linux, or macOS, then appends detected network hardware. Unsupported systems still return the network portion. Platform gatherers use operating-system facilities and command/file parsing; missing permissions or utilities may produce a partial inventory rather than a uniform schema.

`HardwareUtility` normalizes identifiers and values, parses byte quantities, reads the first usable system file, and executes bounded platform commands. These normalization rules are part of profile stability.

> 💡 Persist both the profile identifier and enough individual properties to explain a later mismatch. Treat changes as a risk signal or re-enrollment event, not automatically as hostile activity.

🚨 Hardware details can be personal or security-sensitive. Collect only what the application needs, obtain required consent, restrict access and retention, and never expose raw serial numbers in logs or public telemetry.

## Testing and Troubleshooting

Run the [sample](samples/README.md) on each supported target. Compare native and containerized runs, confirm required permissions, and expect different device sets across cloud instance types. Unit tests should inject contract implementations rather than assuming the CI machine has stable identifiers.

## Related Resources

- [Sample application](samples/README.md)
- [Linux sysfs documentation](https://docs.kernel.org/filesystems/sysfs.html)
- [Implementation guidance](SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

Service scanning exposes the singleton collector as `IHardwareCollector`. Resolve that contract from the host for application use; plugin loading does not require publishing hardware identifiers.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Hardwares` | [Zongsoft.Hardwares.plugin](src/Zongsoft.Hardwares.plugin) |
| File copying and dependencies | [Zongsoft.Hardwares.deploy](src/Zongsoft.Hardwares.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft hardwares]
nuget:Zongsoft.Hardwares
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Hardwares.option`, `Zongsoft.Hardwares.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

# Zongsoft.Externals.Velopack Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Velopack)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Velopack)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Velopack` integrates the [Velopack](https://velopack.io/) desktop application updater with the Zongsoft application and worker lifecycles. It initializes Velopack early, resolves update sources from framework connection settings, periodically checks and downloads a release, then applies it and restarts an installed application.

## Installation and Configuration

```shell
dotnet add package Zongsoft.Externals.Velopack
```

```xml
<option path="/Externals/Velopack">
	<connectionSettings default="current">
		<connectionSetting connectionSetting.name="current"
		                   driver="velopack"
		                   value="source=web;url=https://updates.example.invalid/releases;period=300s" />
	</connectionSettings>
</option>
```

`source` selects an `IVelopackSourceFactory`; `url` and related settings configure it, while `period` controls checks. The initializer configures an `ApplicationLocator` on Windows and Linux. `Upgrader` runs only when Velopack reports a valid installed application and prevents overlapping checks.

## Update Lifecycle

The worker starts a timer, optionally performs an early check after 30 seconds when the regular period is at least five minutes, calls `CheckForUpdatesAsync`, downloads the selected release, and invokes `ApplyUpdatesAndRestart`. Failures are logged and the next period may retry.

> 💡 Development builds not running from a Velopack installation are intentionally ignored. Test the complete installed package, channel and feed rather than expecting `dotnet run` to self-update.

🚨 Applying an update restarts the process and replaces installed files. Sign and publish trusted packages, require HTTPS, protect feed credentials, validate rollback/recovery, coordinate outstanding work, and never point development tests at a production channel.

## Compatibility

Client, package tool and feed schema must use compatible Velopack versions. Test clean install, upgrade from every supported prior version, no-update, interrupted download, corrupted package, insufficient disk/permissions and restart behavior on each target OS.

## References

Velopack documentation
> https://docs.velopack.io

Velopack reference
> https://docs.velopack.io/reference

Velopack source code
> https://github.com/velopack/velopack

The companion [Web feed package](src/web/README.md) publishes release metadata from a host directory.

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

This plugin contributes a startup upgrader. Configure its source and timing only for a Velopack-installed application; enabling it may download updates and restart the process. It is independent of framework/upgrading.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Velopack` | [Zongsoft.Externals.Velopack.plugin](src/Zongsoft.Externals.Velopack.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Velopack.deploy](src/Zongsoft.Externals.Velopack.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals velopack]
nuget:Zongsoft.Externals.Velopack
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Velopack.option`, `Zongsoft.Externals.Velopack.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

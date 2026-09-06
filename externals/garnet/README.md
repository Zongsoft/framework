# Zongsoft.Externals.Garnet Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Garnet)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Garnet)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Garnet` hosts [Microsoft Garnet](https://microsoft.github.io/garnet/) as a Zongsoft `WorkerBase`. Garnet is a high-performance, Redis-protocol-compatible data store; this adapter owns server start/stop and translates framework settings into Garnet command-line options.

## Installation and Configuration

```shell
dotnet add package Zongsoft.Externals.Garnet
```

Deploy the plugin and option artifacts. A named server is configured below `/Externals/Garnet`:

```xml
<option path="/Externals/Garnet">
	<server server.name="cache"
	        value="bind=127.0.0.1;port=6379;auth=Password;password=REPLACE_ME;lua=true" />
</option>
```

Start a `GarnetServer("cache")` through the application worker lifecycle. Its `Setting` property can select a setting independently of the worker name. Relative path-valued options resolve from the adapter assembly directory; `~/` resolves from the application root.

## Lifecycle and Persistence

Starting creates the underlying `Garnet.GarnetServer` and calls `Start`; stopping disposes it. The adapter maps friendly names such as `Address`, `EnableAOF`, `CheckpointDir`, and `EnableTLS` to Garnet options while passing native option names through.

> 💡 Redis protocol compatibility does not imply that every Redis command, module, persistence mode, or client behavior is identical. Validate the exact commands used by the consuming application.

🚨 The packaged example password is a placeholder. Bind to trusted interfaces, set authentication and TLS deliberately, protect checkpoint/AOF directories, restrict module loading, and test recovery before storing durable data. An in-process server shares failure and resource boundaries with its host.

## Verification

Use a disposable directory and port, start the worker, connect with a compatible client, exercise required commands, stop it, and—when persistence is enabled—restart and verify recovery. Test memory limits, connection limits, shutdown time, and corrupted-checkpoint handling before production.

- Configuration file documentation
	> [Garnet defaults](https://github.com/microsoft/garnet/blob/main/libs/host/defaults.conf)

- Configuration option mapping
	> [Garnet option model](https://github.com/microsoft/garnet/blob/main/libs/host/Configuration/Options.cs)

- [External adapter implementation guidance](../SKILL.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

This plugin mounts a startup worker. Configure `/Externals/Garnet` before starting the host: loading the worker can start an embedded server and open a listening port.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Externals.Garnet` | [Zongsoft.Externals.Garnet.plugin](src/Zongsoft.Externals.Garnet.plugin) |
| File copying and dependencies | [Zongsoft.Externals.Garnet.deploy](src/Zongsoft.Externals.Garnet.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft externals garnet]
nuget:Zongsoft.Externals.Garnet
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Externals.Garnet.plugin`, `Zongsoft.Externals.Garnet.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

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

Deploy the plugin and option artifacts. The packaged plugin selects the following server with `setting="default"`; the binding reads only that entry:

```xml
<?xml version="1.0" encoding="utf-8" ?>

<?binding name="garnet"
	path="Externals/Garnet/default"
	template="tcp://${option.Value['address']}"
	protocols="tcp"
	required="true"?>

<options>
	<option path="/Externals/Garnet">
		<server server.name="default"
			value="address=127.0.0.1:6379;auth=Password;password=REPLACE_ME;lua=true" />
	</option>
</options>
```

`GarnetServer.Setting` selects a named entry and falls back to the worker name only when the property is unspecified. A missing entry fails startup without choosing another preset or the SDK's default listener. Additional entries do not start automatically. When changing the plugin's `Setting`, update its binding `path` to select the same entry. `Setting` can only change while the worker is stopped.

Each instance configures one IP and a fixed port through `address`, such as `address=127.0.0.1:6379` or `address=[::1]:6379`. Ports range from 1 through 65535. The adapter converts this value into the native Garnet `bind` and `port` arguments; the binding template reads the same address. Setting keys are case-insensitive. DNS names, address lists, wildcards, paths, query parameters, random ports, and separate `bind/port` settings are not accepted. Choose a reachable listening address explicitly for connections across containers.

`protocols="tcp"` is a constant template; no tool-only option is forwarded to Garnet. Bindings publish TCP connections. With `tls/EnableTLS`, Garnet terminates TLS and the proxy passes the connection through. Cluster announcement options are not listening addresses. This first static binding implementation rejects Unix sockets and native `config-import-path`, including its alias, to prevent additional configuration sources from introducing undeclared listeners. Use framework `<?import ... ?>` directives to compose files into the same option tree.

Relative path-valued options resolve from the adapter assembly directory; `~/` resolves from the current application root. This includes `cert-file-name/CertificateFileName` and `issuer-certificate-path/IssuerCertificatePath`. Enable TLS through Garnet options such as `tls=true` and `cert-file-name=~/certificates/garnet.pfx`.

## Typed Configuration

The [settings object](src/Configuration/GarnetConnectionSettings.cs) and [driver](src/Configuration/GarnetConnectionSettingsDriver.cs) use `ConnectionSettingsBase<TDriver>` and `ConnectionSettingsDriver<TSettings>`. The plugin registers the `Garnet` driver at `/Workbench/Configuration/ConnectionSettings/Drivers`. The server selects its named settings and uses this driver to create the typed configuration.

```csharp
var settings = GarnetConnectionSettingsDriver.Instance.GetSettings(
	"default", "address=127.0.0.1:6379;tls=true;cert-file-name=~/certificates/garnet.pfx;");

settings.EnableAOF = true;
settings.CommitFrequency = TimeSpan.FromSeconds(1);
settings.CheckpointDirectory = "~/.garnet";
```

Import `Zongsoft.Externals.Garnet.Configuration`. Common properties cover Address, AuthenticationMode, Password, TLS and certificates, AOF and its commit interval, Lua, recovery, checkpoints, and quiet mode. Native and friendly names identify the same setting; repeated keys and aliases follow the framework's last-value-wins rule. Other native Garnet parameters remain available without requiring a property for every SDK option.

AuthenticationMode uses the SDK enum `GarnetAuthenticationMode?`, with NoAuth, Password, Aad, ACL, and AclWithAad. Configuration text remains `auth=Password`; runtime arguments use the canonical enum name. An unspecified value remains null rather than the adapter inserting NoAuth.

Duration properties use `TimeSpan?`: `CommitFrequency` maps to integer milliseconds and `CertificateRefreshFrequency` maps to integer seconds. Text may use units, such as `CommitFrequency=1s` and `CertificateRefreshFrequency=1m`; unitless integers retain the corresponding native unit. Values must be exactly representable in that unit and within the native integer range. Zero and the commit interval of -1ms retain their native meanings. Boolean and duration properties are nullable: the adapter does not insert SDK defaults when no explicit value is supplied. Prefer explicit booleans such as `tls=true` and `quiet=true`. A native valueless flag such as `quiet` remains in the dictionary for the SDK to interpret. Address has no default and must be specified before startup.

The settings object owns address validation, native argument conversion, and path resolution. Argument conversion is internal to the assembly; GarnetServer owns settings selection and instance lifecycle.

## Lifecycle and Persistence

Starting creates the underlying `Garnet.GarnetServer` and saves it only after `Start` succeeds. Cancellation or startup failure disposes the created instance; stopping or disposing the worker clears and disposes the server. Startup failures are reported through `StateChanged`, with the worker returning to `Stopped`. Friendly names such as `EnableAOF`, `CheckpointDirectory`, and `EnableTLS` map to native options. Process-level commands such as `help/version` are rejected.

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

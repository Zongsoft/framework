# Zongsoft.Upgrading.Upgrader

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Upgrader)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Upgrader)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README-zh_CN.md)

-----

## Overview

[**Z**ongsoft.**U**pgrading.**U**pgrader](https://github.com/Zongsoft/framework/tree/main/upgrading/upgrader) is the client-side automatic upgrader for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework.

After it is loaded into a Zongsoft host application, it periodically checks configured release channels, downloads matching packages, extracts them, creates the deployment descriptor, starts the deployer executable, and then shuts down the current application so deployment can continue outside the locked process.

## Responsibilities

- Resolve the current application identity: name, edition, version, platform, architecture, application path, and application type.
- Discover candidate releases from the configured `File` or `Web` channel.
- Select full and delta releases newer than the current version and not newer than an optional target version.
- Download release packages into a temporary application-specific directory.
- Verify package size and checksum before deployment.
- Extract packages into a temporary `.app` directory.
- Write the `.deployment` deployment descriptor into the application directory.
- Start `Zongsoft.Upgrading.Deployer` and stop the current host process.

## Installation

Install the NuGet package into the application that should upgrade itself:

```shell
dotnet add package Zongsoft.Upgrading.Upgrader
```

The package also contains the plugin and option artifacts used by Zongsoft plugin hosting:

- `Zongsoft.Upgrading.Upgrader.plugin`
- `Zongsoft.Upgrading.Upgrader.option`
- `Zongsoft.Upgrading.Upgrader.deploy`

## Configuration

The default option file declares two connection settings under `/Upgrading`:

```xml
<options>
	<option path="/Upgrading">
		<connectionSettings default="File">
			<connectionSetting connectionSetting.name="Web"
			                   value="url=http://127.0.0.1:8069/Upgrading/Upgrader;timeout=30s" />

			<connectionSetting connectionSetting.name="File"
			                   value="url=zfs.s3:/upgrading/releases/" />
		</connectionSettings>
	</option>
</options>
```

Setting | Description
-------|------------
`default` | The channel used when `Upgrader.UpgradeAsync()` is called without an explicit channel.
`Web:url` | Base URL of the Web package manager discovery endpoint.
`Web:timeout` | HTTP timeout. Values such as `30s` are parsed by Zongsoft time-span utilities.
`File:url` | Root file-system URL containing release manifests and package files.

## Plugin Startup

The packaged plugin registers an `Upgrader` worker under `/Workbench/Startup`:

```xml
<extension path="/Workbench/Startup">
	<object name="Upgrader" period="10m" type="Zongsoft.Upgrading.Upgrader, Zongsoft.Upgrading.Upgrader" />
</extension>
```

The worker checks for upgrades every `period`. If the period is five minutes or longer, it also schedules one short-delay check about ten seconds after startup.

## Manual Usage

Applications can invoke the upgrader directly:

```csharp
using Zongsoft.Upgrading;

if(await Upgrader.UpgradeAsync("Web", cancellationToken))
	Upgrader.Deploy();
```

`UpgradeAsync` returns `true` when an upgrade was prepared successfully or when a deployment descriptor already exists and is waiting to be deployed.

## Channel Behavior

### File Channel

The `File` channel scans the configured root URL in this order:

1. `{root}/{application-name}/*.manifest`
2. `{root}/{application-type}/{application-name}/*.manifest`
3. `{root}/{application-type}/{application-name}*.manifest`
4. `{root}/{application-name}*.manifest`

For each manifest, it resolves the package path relative to the manifest location and stores the resolved download URL in release properties.

### Web Channel

The `Web` channel calls:

```text
{base-url}/{application-name}/{edition}?Name=...&Edition=...&Platform=...&Architecture=...&CurrentlyVersion=...
```

The request also includes hardware fingerprint information that server-side evaluators can use to decide whether a release is suitable for a specific instance.

## Release Selection

The upgrader only accepts releases that match:

- application name;
- platform;
- architecture;
- edition, when specified;
- version greater than the current application version;
- optional target version, when specified.

When a full release is available, the newest full release is selected as the trunk and only delta releases newer than that trunk are applied. If no full release is selected, matching delta releases are applied in ascending version order.

## Deployment Hand-Off

The deployer executable must be available at:

```text
{application-path}/.deployer/Zongsoft.Upgrading.Deployer
```

On Windows the executable is `Zongsoft.Upgrading.Deployer.exe`. On Linux, the upgrader starts the deployer with `systemd-run` so it can continue after the application process exits.

The upgrader passes the deployer these key arguments:

Argument | Description
--------|------------
`site` | Site identifier from application configuration.
`app.id` | Current process id.
`app.name` | Current application name.
`app.type` | Application type, such as `Web`, `Daemon`, or `Terminal`.
`app.path` | Application root directory.
`host.path` | Current host executable path.
`host.args#n` | Original host command-line arguments.
`daemon` | Optional service name used by deployer restart logic.
`deployment` | Full path of the `.deployment` descriptor file.

## Executors

The upgrader registers the built-in executor commands:

- `Copy`
- `Move`
- `Link`
- `Delete`

Executors are declared in the release manifest and are later invoked by the deployer during `Deploying` and `Deployed`.

## Notes

- The upgrade package should include a deployer executable compatible with the target platform.
- Full releases clean the application directory during deployment; keep persistent data, logs, and configuration outside the files replaced by the upgrade package or exclude them from the package.
- The running host must be able to shut down cleanly within the upgrader shutdown timeout.

## License

This project is licensed under the [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) license.

# Zongsoft.Upgrading.Deployer

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Upgrading.Deployer)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Upgrading.Deployer)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**U**pgrading.**D**eployer](https://github.com/Zongsoft/framework/tree/main/upgrading/deployer) is the client-side deployment executable for automatic upgrades. It is published as a standalone _**N**ative **AOT**_ program so it can keep running after the host application exits.

The deployer receives the deployment descriptor created by the upgrader, waits until the host process has stopped, copies the extracted package files into the application directory, invokes deployment executors, restarts the application, and then exits.

## Responsibilities

- Parse command-line arguments passed by the upgrader.
- Wait for the host process to exit before touching application files.
- Load the `.deployment` descriptor and lock it exclusively during deployment.
- Load the release manifest referenced by the descriptor.
- Clean the application directory for full releases while preserving the deployer directory and deployment descriptor.
- Copy extracted package files into the application directory.
- Invoke `Deploying` and `Deployed` executor commands from the release manifest.
- Restart the host application according to its application type.

## Deployment Descriptor

The upgrader writes a `.deployment` file into the application directory. The deployer receives its full path through the `deployment` argument.

```text
Manifest=C:\Users\...\Temp\Zongsoft.Hosting.Terminal\Zongsoft.Hosting.Terminal@1.1.0.manifest
Packages=C:\Users\...\Temp\Zongsoft.Hosting.Terminal\.app
```

Field | Description
-----|------------
`Manifest` | Full path of the release manifest saved by the upgrader.
`Packages` | Directory containing the extracted release package files.

When the deployer opens this file for deployment it uses an exclusive lock and deletes the descriptor on close.

## Command-Line Arguments

Argument | Required | Description
--------|:--------:|------------
`deployment` | ✓ | Full path of the `.deployment` descriptor.
`app.id` | - | Host process id. When provided, the deployer waits for this process to exit.
`app.name` | - | Application name.
`app.type` | - | Application type used by restart logic, such as `Web`, `Daemon`, or `Terminal`.
`app.path` | - | Application root directory.
`host.path` | - | Host executable path.
`host.args#n` | - | Original host command-line arguments.
`site` | - | Site identifier.
`daemon` | - | Service name used by daemon or web restart logic.

Example:

```shell
Zongsoft.Upgrading.Deployer \
	app.id=12345 \
	app.name=Zongsoft.Hosting.Terminal \
	app.type=Terminal \
	app.path=/opt/zongsoft/terminal \
	host.path=/opt/zongsoft/terminal/Zongsoft.Hosting.Terminal \
	deployment=/opt/zongsoft/terminal/.deployment
```

## Deployment Behavior

Release kind | Behavior
------------|---------
`Fully` | The application directory is cleaned first, except for the deployer directory and the `.deployment` descriptor, then extracted files are copied in.
`Delta` | Extracted files are copied over the existing application directory without first cleaning it.

The deployer directory is named `.deployer`; the expected executable name is:

- `Zongsoft.Upgrading.Deployer.exe` on Windows;
- `Zongsoft.Upgrading.Deployer` on Linux and other Unix-like platforms.

## Restart Strategy

Application type | Windows | Linux or FreeBSD
----------------|---------|-----------------
`Web` | Recycles an IIS application pool through `appcmd recycle apppool {app.name}`. | Starts the service through `systemctl start {daemon}`.
`Daemon` | Starts the service through `sc start {daemon}`. | Starts the service through `systemctl start {daemon}`.
`Terminal` | Starts the original host executable with its original arguments. | Starts the original host executable with its original arguments and waits for it to exit so console input remains usable.
Other | Starts the original host executable with its original arguments. | Starts the original host executable with its original arguments.

If `daemon` is not provided, the deployer tries to find a single `*.service` file in the application directory, then a service file whose name starts with `app.name`; if neither is found, it falls back to `app.name`.

## Executors

The deployer registers these built-in commands before deployment:

- `Copy`
- `Move`
- `Link`
- `Delete`

Release manifest executors are invoked during:

- `Deploying`: after cleanup, before package files are copied;
- `Deployed`: after package files have been copied, before the host application is restarted.

## Publishing

### Windows

#### Publish With Visual Studio

1. Open the [Zongsoft.Upgrading.Deployer.slnx](./Zongsoft.Upgrading.Deployer.slnx) solution with _**M**icrosoft **V**isual **S**tudio 2026_.
2. Select the project in _Solution Explorer_, right-click it, and choose _Publish_.
3. Click _Publish_ in the publish window.

#### Publish From The Command Line

Run the following command in the `deployer` directory:

```cmd
dotnet publish "Zongsoft.Upgrading.Deployer.csproj" ^
  -c Release ^
  -f net10.0 ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:PublishReadyToRun=true ^
  -p:PublishAot=true
```

The published executable is written to:

```text
bin\Release\net10.0\win-x64\publish\Zongsoft.Upgrading.Deployer.exe
```

### Linux

#### Publish Inside The Container

1. Run the [framework.start](./../../framework-start.cmd) script to start the _**L**inux_ Alpine container that contains the _.NET 10 SDK_.

	Make sure the container has finished loading:

	```cmd
	podman ps -a --pod
	podman logs zongsoft-framework
	```

2. Enter the `zongsoft-framework` container:

	```cmd
	podman exec --workdir /Zongsoft/framework/upgrading/deployer -it zongsoft-framework sh
	```

3. Publish the _**L**inux_ `x64` deployer:

	```shell
	./publish.linux-x64.sh
	```

4. Run `exit` to leave the container.
5. Run the [framework.stop](./../../framework-stop.cmd) script to stop the container.

#### Publish Outside The Container

1. Run the [framework.start](./../../framework-start.cmd) script to start the _**L**inux_ Alpine container that contains the _.NET 10 SDK_.

	Make sure the container has finished loading:

	```cmd
	podman ps -a --pod
	podman logs zongsoft-framework
	```

2. Run one of the publish scripts:

	- [publish.linux-x64.ps1](./publish.linux-x64.ps1) in _**P**ower**S**hell_;
	- [publish.linux-x64.cmd](./publish.linux-x64.cmd) in _CMD_.

3. Run the [framework.stop](./../../framework-stop.cmd) script to stop the container.

## Notes

- The deployer must not be overwritten by a full release. Keep it under the `.deployer` directory so cleanup and copy logic can preserve it.
- The deployer waits for the host process for up to about 60 seconds.
- Service restart commands usually require elevated permissions.

## License

This project is licensed under the [MIT](https://github.com/Zongsoft/framework/blob/main/LICENSE) license.

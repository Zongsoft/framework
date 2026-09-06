# Zongsoft.Commands Command Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Commands)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Commands)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**C**ommands](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Commands) packages commonly needed terminal commands for the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) command framework. It is useful for application diagnostics, maintenance scripts, interactive administration, and testing framework services without writing a dedicated user interface.

The package contributes commands through [`Zongsoft.Commands.plugin`](src/Zongsoft.Commands.plugin). After the plugin is loaded, they appear below `/Workbench/Executor/Commands` and can be invoked from a Zongsoft terminal host or through `CommandExecutor`.

## Command Model

A [command](../Zongsoft.Core/src/Components/ICommand.cs) receives a parsed expression through a command context and returns a value asynchronously. The framework separates four concerns:

- **Command tree**: hierarchical names such as `File Info` or `Messaging Queue Produce` select a command node.
- **Arguments and options**: positional arguments carry input values; named options use forms such as `--algorithm:SHA256`.
- **Pipeline value**: the result of one command can become the input value of another command.
- **Output**: commands may return an object and may also write formatted content through `ICommandOutlet`.

See [`CommandExecutor`](../Zongsoft.Core/src/Components/CommandExecutor.cs), [`CommandContext`](../Zongsoft.Core/src/Components/CommandContext.cs), and [`CommandLine`](../Zongsoft.Core/src/Components/CommandLine.cs) for the underlying public abstractions.

## Installation and Deployment

```shell
dotnet add package Zongsoft.Commands
```

For a plugin application, deploy the package artifacts so that `Zongsoft.Commands.plugin` is placed in the host's `plugins` directory. The manifest depends on the framework's main plugin and registers the command tree automatically.

> 💡 The easiest interactive entry point is the [Zongsoft terminal host](https://github.com/Zongsoft/hosting/tree/main/terminal). Use `help` after startup to inspect the commands actually loaded by the current application.

## Quick Start

```text
help
range --type:int --min:1 --max:5
checksum --algorithm:SHA256 "hello"
file.info ./appsettings.json
directory.list ./plugins
```

Application code can invoke the same tree after plugin initialization:

```csharp
using Zongsoft.Components;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var checksum = await CommandExecutor.Default.ExecuteAsync(
	"checksum --algorithm:SHA256 hello",
	cancellation: cancellation.Token);
```

> 🚨 `CommandExecutor.Default` only contains nodes registered by the current host. A successful package reference does not load `Zongsoft.Commands.plugin`; verify deployment when a command cannot be found.

## Included Commands

| Group | Commands | Purpose |
| --- | --- | --- |
| General | `Help`, `Echo`, `Cast`, `Dump`, `Json`, `Range`, `Random`, `Shuffle` | Inspect, convert, format, and generate values. |
| Runtime | `Assembly`, `Checksum` | Inspect assemblies and compute checksums. |
| File system | `File Open/Save/Copy/Move/Info/Exists/Delete`, `Directory List/Move/Info/Exists/Delete` | Work with the framework file-system abstraction. |
| Security | `RSA Export/Import`, `Secret Generate/Verify`, `Password Generate/Parse` | Manage keys, secrets, and password representations. |
| Scheduling | `Scheduler`, `Schedule`, `Reschedule`, `Unschedule` | Inspect and change registered schedules. |
| Sequence | `Sequence Info/Reset/Increase/Decrease` | Operate on the configured sequence provider. |
| Configuration | `Configuration Get` | Read the effective application configuration. |
| Messaging | `Messaging Queue Produce/Subscribe` | Publish and observe messages through a configured queue provider. |

Some commands require services supplied by other plugins. Scheduling, sequence, messaging, and virtual file-system commands resolve their providers from the running application.

## Extending the Tree

Custom commands normally derive from `CommandBase<CommandContext>`, declare options with `CommandOptionAttribute`, and are mounted by a plugin manifest. Keep command bodies thin: validate terminal input, call an application service, and return a useful result rather than duplicating domain logic.

> 💡 Use a parent command node to group related operations. This keeps completion and `help` output discoverable while avoiding long flat command names.

## Errors and Safety

- Invalid or missing options are reported as command option exceptions; do not silently substitute security-sensitive values.
- File deletion, key export, scheduling changes, and message publication can alter external state. Confirm the active host, environment, and provider before invoking them.
- Cancellation is propagated to asynchronous commands, but an external provider may already have accepted an operation.
- Avoid passing credentials directly in expressions because terminal history and diagnostic output may retain them.

## Related Resources

- [Core command abstractions](../Zongsoft.Core/src/Components)
- [Command plugin manifest](src/Zongsoft.Commands.plugin)
- [Deployment manifest](src/Zongsoft.Commands.deploy)
- [Plugin framework](../Zongsoft.Plugins/README.md)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../Zongsoft.Plugins/README.md).

The manifest mounts the command set at `/Workbench/Executor/Commands`. In a terminal host, verify `help` and `echo hello`; provider-dependent commands still require their corresponding plugins.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Commands` | [Zongsoft.Commands.plugin](src/Zongsoft.Commands.plugin) |
| File copying and dependencies | [Zongsoft.Commands.deploy](src/Zongsoft.Commands.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft commands]
nuget:Zongsoft.Commands
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Commands.plugin`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

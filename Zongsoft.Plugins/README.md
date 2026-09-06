# Zongsoft.Plugins Plugin Framework

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Plugins)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Plugins)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)


[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**P**lugins](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Plugins) is the application composition layer of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. A small host loads declarative plugin manifests, builds an extension tree, registers services, and starts application modules. Business libraries can remain independent of the plugin runtime; only the host and composition package need to understand plugin loading.

The framework supports terminal applications, background services, Web hosts, and rich clients. It is designed for systems whose features must be packaged, enabled, replaced, or deployed independently.

## Core Concepts

### Plugin Manifest

A `*.plugin` file is an XML composition contract. It identifies a plugin, declares assembly and plugin dependencies, and contributes objects to extension paths. The format is described by [`Zongsoft.Plugins.xsd`](Zongsoft.Plugins.xsd).

### Plugin Tree

Extensions are organized as a path-addressable tree. Framework services are commonly mounted under `/Workbench`; for example, the main manifest exposes application modules, services, the file-system registry, events, the command executor, connection-setting drivers, and diagnostic loggers.

### Builders and Parsers

Builders create nodes such as `object`, `lazy`, and `expose`. Parsers resolve values such as `path`, `type`, `static`, `option`, `service`, and `resource`. A value expression is evaluated when its node is built, so dependency order and path context matter.

### Application Context

`PluginApplicationContext` represents the running application and exposes its environment, modules, services, events, workers, and current principal. Host-specific packages provide the concrete context while business modules interact through Core application abstractions.

## Installation

```shell
dotnet add package Zongsoft.Plugins
```

A deployable host also needs the main plugin manifest. The NuGet package includes the standard deployment artifacts; place deployed `*.plugin` files under the application's `plugins` directory.

## Creating a Host

The hosting helpers integrate plugin initialization with [.NET Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host):

```csharp
using Microsoft.Extensions.Hosting;
using Zongsoft.Plugins.Hosting;

await Application.Terminal(args).RunAsync();
```

Use `Application.Daemon(args)` for a background service. Web applications use the companion [Zongsoft.Plugins.Web](../Zongsoft.Plugins.Web/README.md) package.

The builder loads application configuration, discovers the plugin directory, builds the plugin tree, registers modules and services, initializes the application context, and then starts the host.

## Declaring a Plugin

```xml
<?xml version="1.0" encoding="utf-8" ?>
<plugin name="Acme.Inventory" title="Inventory Module">
	<manifest>
		<dependencies>
			<dependency name="Main" />
		</dependencies>
		<assemblies>
			<assembly name="Acme.Inventory" />
		</assemblies>
	</manifest>

	<extension path="/Workbench/Modules">
		<object name="Inventory" type="Acme.Inventory.Module, Acme.Inventory" />
	</extension>
</plugin>
```

The `name` used by a dependency must match the target plugin's name exactly. Assembly files must be present in the deployed plugin location or resolvable by the host.

> 🚨 A project that compiles successfully can still fail during plugin loading because manifests are runtime contracts. Keep plugin names, dependencies, assembly names, extension paths, and deployment files synchronized.

## Plugin Deployment: From Host to Running Features

### Three Separate Responsibilities

| Step | What it does | What it does not do |
| --- | --- | --- |
| NuGet/project reference | Compiles the host or a business library against APIs | Does not assemble the host's plugin directory |
| Deployment | Copies manifests, assemblies, dependencies, options, mappings and resources to their runtime locations | Does not start the application or initialize a database |
| Plugin loading | Reads manifests, resolves dependencies, builds nodes and registers services in the host | Does not download missing packages |

A host is a launcher, not a business module. Ready-made terminal, daemon and Web launchers are maintained in [Zongsoft/hosting](https://github.com/Zongsoft/hosting). Their deployment manifests compose features independently of the host's source code.

### A Complete Local Terminal Example

Use a disposable working directory with .NET 10 SDK. These are instructions for the reader, not steps to run against an existing application:

```shell
dotnet new console -n PluginDemo -f net10.0
cd PluginDemo
dotnet add package Zongsoft.Plugins
```

Replace Program.cs with this complete entry point:

```csharp
using Microsoft.Extensions.Hosting;
using Zongsoft.Plugins.Hosting;

Application.Terminal("PluginDemo", args).Run();
```

Create a project-local `.deploy` with the following contents. The space-separated section is a destination path:

```ini
[plugins]
nuget:Zongsoft.Plugins/plugins/Main.plugin
nuget:Zongsoft.Plugins/plugins/Terminal.plugin

[plugins zongsoft commands]
nuget:Zongsoft.Commands
```

Install [the deployment tool](https://github.com/Zongsoft/tools/tree/main/deployer) if needed, publish the launcher, then deploy its features:

```shell
dotnet tool install -g Zongsoft.Tools.Deployer
dotnet publish -c Release -f net10.0 -o out
dotnet deploy --destination:./out --framework:net10.0 --edition:Release --platform:win --architecture:x64
cd out
dotnet PluginDemo.dll
```

The platform/architecture values above describe a Windows x64 example; use the actual target for native-dependent plugins. Select compatible package versions and pin them with `package@version` for reproducible deployments. Do not install the tool again if it is already available.

At the interactive prompt, run `help`, `echo hello` and `plugin.list`. The host did not reference Zongsoft.Commands in its project; that feature was added through deployment and its manifest. Use `exit` to stop it.

The relevant output layout is:

```text
out/
	PluginDemo.dll
	PluginDemo.deps.json
	PluginDemo.runtimeconfig.json
	plugins/
		Main.plugin
		Terminal.plugin
		zongsoft/
			commands/
				Zongsoft.Commands.plugin
				Zongsoft.Commands.dll
```

Other host dependencies and satellite resources are omitted from this diagram, not from deployment.

### Adding a Business Plugin

Keep business code in its own class library. Its manifest declares its assembly, dependencies and extension contributions; the earlier Inventory manifest illustrates this structure and requires an application-defined Inventory.Module type. Deploy both that library and its manifest under `plugins`, with options/mappings alongside as required. The host's Program.cs stays unchanged.

The filesystem hierarchy establishes parent/child plugin relationships; extension paths such as `/Workbench/Modules` are a different, logical hierarchy. Do not flatten an existing deployment. Dependency names are resolved across the loaded tree and compared case-insensitively; retain canonical spelling and avoid duplicate names.

### Deployment Manifest versus Source Build

A package's root `.deploy` normally refers to package-relative `artifacts/` and `lib/$(Framework)/`. Execute it through a `nuget:Package` entry; copying this text into a source directory does not make those package paths exist.

For local debugging, stop the test host, build the matching configuration/TFM, then copy the changed assembly plus matching manifest/options/mappings/resources to its existing plugin location and restart. A local application `.deploy` may instead reference explicit source build output paths. Do not run a whole deployment merely to replace one debug DLL: it may overwrite local configuration or remove manually installed files.

The deployer prefers a package-root `.deploy` when no package-internal path is specified. It selects compatible TFM assets and processes nested entries. Its default dependency downloader skips `System.*`, `Microsoft.Extensions.*` and `Zongsoft.*`; therefore explicitly compose required Zongsoft plugins and keep the host runtime dependencies compatible.

### Configuration Is Part of the Composition

The plugin configuration provider loads same-stem option files next to each manifest: base `Feature.option`, environment `Feature.development.option`, then matching environment suffix files and host/site-specific files. Host-level `web.option` or application-name options are loaded separately. The [provider source](src/Configuration/PluginConfigurationProvider.cs) defines exact filename matching; do not assume every arbitrary `.option` file is scanned.

Deployment variables and runtime options are different. `--site:daemon` passed to the deployer can select a `*-daemon.plugin` artifact; `site=daemon` passed to the host participates in runtime configuration selection. Some plugins use that extra manifest to start workers. Merely installing the primary DLL does not enable such workers.

Do not depend on an incidental ordering of multiple plugins defining the same configuration key. Give environment/site settings a clear owner and verify the effective value without printing secrets.

### Verification and Operational Risks

1. Check that the intended host content root contains `plugins/Main.plugin` and each feature's manifest.
2. Check plugin names, dependencies, assembly versions, TFM and native runtime architecture.
3. Inspect `plugin.list`/`plugin.tree`, then verify the named service, driver, command or Web endpoint contributed by that plugin.
4. Verify effective configuration and only then connect to an authorized test service.

🚨 Deployment copies and may overwrite files; manifests can also include delete entries. Back up local configuration, inspect the resolved destination and stop the host before replacement. Plugins run with the host's privileges and are not a security sandbox. Loading configuration with file-change support is not a guarantee of safe DLL hot replacement.

See the [host deployment examples](https://github.com/Zongsoft/hosting) and [deployer syntax and options](https://github.com/Zongsoft/tools/tree/main/deployer) for larger multi-plugin applications.

## Complete Consumer Plugin: Depend Only on Contracts

This example adds a calculation command to the PluginDemo terminal host above. The consumer library references only Core; deployment selects the language provider. The host entry point does not change.

### 1. Implement the Application Module

Create an `Acme.Rules` class library next to PluginDemo:

```shell
dotnet new classlib -n Acme.Rules -f net10.0
cd Acme.Rules
dotnet add package Zongsoft.Core
```

Select a Core version compatible with the host. Save this complete code as `EvaluateCommand.cs`, then run `dotnet build -c Debug`:

```csharp
using Zongsoft.Components;
using Zongsoft.Expressions;
using Zongsoft.Services;

[assembly: ApplicationModule("Rules")]

namespace Acme.Rules;

public sealed class Module : ApplicationModule
{
	public static readonly Module Current = new();
	private Module() : base("Rules") { }
}

public sealed class EvaluateCommand : CommandBase<CommandContext>
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var name = ApplicationContext.Current.Configuration["Rules:Evaluator"]
			?? throw new InvalidOperationException("Rules:Evaluator is missing.");
		var evaluator = Module.Current.Services.FindRequired<IExpressionEvaluator>(name);
		var result = evaluator.Evaluate("x + y", new Dictionary<string, object>
		{
			["x"] = 20,
			["y"] = 22,
		});
		context.Output.WriteLine(result);
		return ValueTask.FromResult(result);
	}
}
```

`Module.Current` is an entry point defined by this application. The assembly attribute identifies service ownership, while the manifest adds the module to the current application. Its container searches module registrations before falling back to shared application services; providers do not need to be registered again for every module.

### 2. Declare the Manifest and Options

Create `Acme.Rules.plugin` in the library directory:

```xml
<?xml version="1.0" encoding="utf-8"?>
<plugin name="Acme.Rules">
	<manifest>
		<assemblies>
			<assembly name="Acme.Rules" />
		</assemblies>
		<dependencies>
			<dependency name="Main" />
		</dependencies>
	</manifest>
	<extension path="/Workbench/Modules">
		<object name="Rules" value="{static:Acme.Rules.Module.Current, Acme.Rules}" />
	</extension>
	<extension path="/Workbench/Executor/Commands">
		<object name="Evaluate" type="Acme.Rules.EvaluateCommand, Acme.Rules" />
	</extension>
</plugin>
```

Create `Acme.Rules.option` beside it:

```xml
<options>
	<option path="/">
		<rules evaluator="Scriban" />
	</option>
</options>
```

The `evaluator` **attribute** on `rules` produces `Rules:Evaluator`. This configuration provider treats XML text nodes as collection items; `<evaluator>Scriban</evaluator>` does not produce the same scalar key.

### 3. Deploy and Invoke

Append to PluginDemo's existing `.deploy`:

```ini
[plugins zongsoft externals scriban]
nuget:Zongsoft.Externals.Scriban

[plugins acme rules]
../Acme.Rules/bin/Debug/net10.0/Acme.Rules.dll
../Acme.Rules/Acme.Rules.plugin
../Acme.Rules/Acme.Rules.option
```

Stop the host, rerun the deployment command above from the PluginDemo project directory, then start the host from `out` and enter `evaluate`. The expected output is `42`. Scriban and Acme.Rules are separate implementation and consumer plugins; consumer code does not need `using Zongsoft.Externals.Scriban`.

This example performs local arithmetic without a database or cloud service. Exit with `exit -yes`; remove only the sample deployment you created, never an existing business host.

### 4. Injection, Selection, and Ownership

For a business object with a public `IExpressionEvaluator Evaluator { get; set; }` property, the composition layer can use `Evaluator="{service:Scriban@}"` to inject the named evaluator from the application container. `{service:@Rules}` returns the Rules module container itself. XML selects and composes implementations while business code stays contract-based.

Switching configuration is valid only when both implementations satisfy the same contract **and application semantics**. Lua has its own expression syntax; changing a provider name does not make arbitrary Scriban scripts portable. Evaluators are shared registrations and are not disposed per lookup. For named cache/sequence instances, use the provider's `GetService(name)` or `Locate<T>("name@provider")`; see [Core service lookup](../Zongsoft.Core/README.md) and the [complete Redis example](../externals/redis/README.md).

### Local Verification and Its Limits

Isolated .NET 10 Windows deployments using the Zongsoft and Automao terminal launchers verified configuration reading, named Scriban lookup, the result `42`, module fallback to a shared instance, plugin property injection, and named Redis cache reads/writes/cleanup. The verification consumer referenced only Core contracts and did not change host business code. This does not certify all plugins or real business workflows.

- Windows terminal hosts and the current deployer require valid console handles. Automation should allocate PTY/ConPTY; ordinary pipes are not equivalent to interactive consoles.
- Start from the deployment directory or explicitly configure the content root. An absolute DLL path does not change the working directory.
- Preserve dependency subdirectories such as `runtimes/` when copying host output. Check the host-root Core version when updating plugins compiled against Core.
- The deployer can exit with code 0 while reporting entry errors. Inspect errors, expected files, and the first contract-level operation.

## Loading and Lifecycle

1. Discover manifests and resolve plugin dependencies.
2. Load declared assemblies and register application services.
3. Build extension nodes when required by the tree.
4. Initialize the application context and modules.
5. Start workers and the underlying host.
6. Stop workers and dispose constructed components when the host shuts down.

Construction failures include the plugin path and component context. Avoid side effects in constructors; perform startable work through application modules or workers so shutdown and failure cleanup remain deterministic.

## Configuration

`PluginConfigurationSource` makes plugin options available through Microsoft configuration. Keep responsibilities distinct:

- `*.plugin` composes plugins and extension nodes.
- `*.option` supplies application options.
- `*.mapping` describes Data entities and commands.
- `*.deploy` controls package deployment.

> 💡 Start from [`plugins/Main.plugin`](plugins/Main.plugin) for the standard workbench tree and [`plugins/Terminal.plugin`](plugins/Terminal.plugin) for terminal-specific composition.

## Diagnostics

- Use the plugin `Tree`, `List`, and `Find` commands to inspect the effective extension tree.
- Check the first missing dependency or assembly error rather than later component failures.
- Treat paths as contextual: relative path expressions are evaluated from the current node.
- Verify that configuration and deployment artifacts were copied next to the plugin manifest.

## Related Resources

- [Plugin XML schema](Zongsoft.Plugins.xsd)
- [Main plugin example](plugins/Main.plugin)
- [Terminal plugin example](plugins/Terminal.plugin)
- [Web plugin hosting](../Zongsoft.Plugins.Web/README.md)
- [.NET Generic Host](https://learn.microsoft.com/dotnet/core/extensions/generic-host)

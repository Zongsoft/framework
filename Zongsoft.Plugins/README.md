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

A `*.plugin` file is an XML composition contract. It identifies a plugin, declares assembly and plugin dependencies, and contributes objects to extension paths. The format is described by [`Zongsoft.Plugins.xsd`](Zongsoft.Plugins.xsd). Extensions are optional and repeatable. The `manifest`, `builders` and `parsers` sections are each optional and may occur at most once. When present, `builders` must contain at least one `builder`, and `parsers` at least one `parser`. Sections follow the order `manifest` → `builders` → `parsers` → `extension`. The schema validates standard declarations and nested `object`/`expose` components while allowing dynamic properties and custom builders. Missing-schema warnings for member elements such as `QueueServer.Storages` indicate the limits of static validation; see the [validation boundary](implementation.md#5-manifest-format-and-declaration-model).

### Plugin Tree

Extensions are organized as a path-addressable tree. Framework services are commonly mounted under `/Workbench`; for example, the main manifest exposes application modules, services, the file-system registry, events, the command executor, connection-setting drivers, and diagnostic loggers.

### Builders and Parsers

Builders create nodes such as `object` and `expose`. Parsers resolve values such as `path`, `type`, `static`, `option`, `service`, and `resource`. A value expression is evaluated when its node is built, so dependency order and path context matter. Property assignment resolves expressions or referenced nodes before converting the result with the target member type and converter. This also applies to string and object properties; expressions such as `{option:...}` are not assigned as raw text. The resolved value may already be cached before the member type and converter are known, so assignment still attempts conversion. A null result is preserved rather than converted to a value type's default, which could mask a failed conversion.

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

await (await Application.TerminalAsync(args)).RunAsync();
```

Use `Application.DaemonAsync(args)` for a background service. Web applications use the companion [Zongsoft.Plugins.Web](../Zongsoft.Plugins.Web/README.md) package.

The builder loads application configuration, discovers the plugin directory, builds the plugin tree, registers modules and services, initializes the application context, and then starts the host.

## Terminal signals and shutdown

The default lifetime used by `Application.TerminalAsync` registers SIGINT, SIGQUIT and SIGTERM during startup and removes registrations when the host is asynchronously disposed. Core terminals only provide the `ITerminal.Abort()` notification.

- Ctrl+C / SIGINT first notifies the running executor's terminal. Setting `Aborting` arguments to `Cancel = true` keeps the host running; an unhandled request stops it. `ReactiveAsync` uses this notification to finish its wait, await cleanup and resume accepting commands.
- Ctrl+Break / SIGQUIT and SIGTERM directly request host shutdown and cannot be vetoed by `Aborting`. Shutdown cancels the terminal run token and waits for the input caller, command cleanup and workers to finish. The process-wide standard input read does not delay host shutdown.
- Repeated signals during shutdown neither repeat abort notifications nor force immediate termination. Abort handlers must return promptly and must not wait synchronously for their own host to stop. Handler failures request shutdown and propagate through `StopAsync` and asynchronous disposal.

An explicitly registered `IHostLifetime` is preserved and owns its signal policy. One active host should coordinate console signals for the process. Status messages use the `Microsoft.Hosting.Lifetime` logging category and honor application logging providers, filters and `ConsoleLifetimeOptions.SuppressStatusMessages`. The terminal host does not change the application's console input mode.

Use `RunAsync`, or explicitly call `StartAsync`, `StopAsync` and asynchronously dispose the host, to await cleanup. Forced operating-system termination is outside cooperative shutdown.

## Declaring a Plugin

Use the real [Discussions business manifest](../../discussions/src/Zongsoft.Discussions.plugin). This excerpt shows assemblies, dependencies and module nodes; it is not the complete manifest. Preserve the original validators, data filters and identity extensions when deploying:

```xml
<manifest>
	<assemblies>
		<assembly name="Zongsoft.Discussions" />
	</assemblies>
	<dependencies>
		<dependency name="Zongsoft.Data" />
		<dependency name="Zongsoft.Security" />
	</dependencies>
</manifest>

<extension path="/Workbench/Modules">
	<object name="Discussions" value="{static:Zongsoft.Discussions.Module.Current, Zongsoft.Discussions}">
		<expose name="Accessor" value="{path:../@Accessor}">
			<expose name="Filters" value="{path:../@Filters}" />
		</expose>
		<expose name="Events" value="{path:../@Events}" />
		<expose name="Properties" value="{path:../@Properties}" />
	</object>
</extension>
```

The plugin is named `Zongsoft.Discussions`; its module is `Discussions`. These names are not interchangeable. See the [project file](../../discussions/src/Zongsoft.Discussions.csproj) and [Module.cs](../../discussions/src/Module.cs). The business library references Core contracts; runtime plugin dependencies supply data-engine and security implementations.

🚨 Do not replace the complete manifest with this excerpt: that would omit site constraints, post filtering and identity transformation. A successful build does not prove that dependency plugins, assemblies and configuration were deployed correctly.

## Plugin Deployment: From Host to Running Features

### Three Separate Responsibilities

| Step | What it does | What it does not do |
| --- | --- | --- |
| NuGet/project reference | Compiles the host or a business library against APIs | Does not assemble the host's plugin directory |
| Deployment | Copies manifests, assemblies, dependencies, options, mappings and resources to their runtime locations | Does not start the application or initialize a database |
| Plugin loading | Reads manifests, resolves dependencies, builds nodes and registers services in the host | Does not download missing packages |

A host is a launcher, not a business module. Ready-made terminal, daemon and Web launchers are maintained in [Zongsoft/hosting](https://github.com/Zongsoft/hosting). Their deployment manifests compose features independently of the host's source code.

### Deployment Composition from Real Projects

[Main.plugin](plugins/Main.plugin) and [Terminal.plugin](plugins/Terminal.plugin) are the framework's existing base manifests. Discussions ships its forum module through the [domain deployment manifest](../../discussions/src/Zongsoft.Discussions.deploy) and [Web deployment manifest](../../discussions/src/api/Zongsoft.Discussions.Web.deploy).

Add this composition fragment to a compatible existing test host's deployment plan. It specifies destinations for the two business packages, not the host's complete manifest:

```ini
[plugins zongsoft discussions]
nuget:Zongsoft.Discussions

[plugins zongsoft discussions web]
nuget:Zongsoft.Discussions.Web
```

The domain package contains `Zongsoft.Discussions.dll` and matching `.plugin`, `.option`, and `.mapping` files. The Web package contains `Zongsoft.Discussions.Web.dll`, its manifest and archive templates. Their package-root `.deploy` files copy from `artifacts/` and `lib/${Framework}/`; these are package paths, not source-directory paths.

### Adding the Business Plugin

The host owns startup, Discussions owns the forum domain, and the Web package owns HTTP adaptation. Explicitly compose Data, Security, a database driver and the selected file-storage provider too; the two business-package entries alone are not a complete dependency set. Connection, site and storage prerequisites are explained in the [real Data workflow](../Zongsoft.Data/README.md#plugin-quickstart).

Filesystem nesting differs from logical extension paths such as `/Workbench/Modules`. Preserve deployment layout and manifest dependencies. After startup, inspect the plugin tree and the `Discussions` module before exercising actual APIs; do not hard-code business dependencies into the host entry point.

💡 Use an isolated deployment, test database and test identity. Startup and deployment commands belong to the [existing host guide](https://github.com/Zongsoft/hosting) and [deployment tool](https://github.com/Zongsoft/tools/tree/main/deployer); this guide does not introduce a host project absent from the repository.

### Deployment Manifest versus Source Build

A package's root `.deploy` normally refers to package-relative `artifacts/` and `lib/${Framework}/`. Execute it through a `nuget:Package` entry; copying this text into a source directory does not make those package paths exist.

For local debugging, stop the test host, build the matching configuration/TFM, then copy the changed assembly plus matching manifest/options/mappings/resources to its existing plugin location and restart. A local application `.deploy` may instead reference explicit source build output paths. Do not run a whole deployment merely to replace one debug DLL: it may overwrite local configuration or remove manually installed files.

The deployer prefers a package-root `.deploy` when no package-internal path is specified. It selects compatible TFM assets and processes nested entries. Its default dependency downloader skips `System.*`, `Microsoft.Extensions.*` and `Zongsoft.*`; therefore explicitly compose required Zongsoft plugins and keep the host runtime dependencies compatible.

### Configuration Is Part of the Composition

The plugin configuration provider selects one entry beside each manifest: first the `.option` with the same basename as the `.plugin` file, then `<plugin name="…">.option` only if the first file is absent. For example, `Feature-daemon.plugin` with plugin name `Feature.Daemon` selects `Feature-daemon.option`, falling back to `Feature.Daemon.option`. If neither exists, no configuration is loaded. The entries are not merged; an empty or invalid first entry does not trigger fallback. Invalid XML fails loading.

The provider does not scan suffixes, environment names, host names, sites or subdirectories. Fragments require explicit `<?import path="…" required="false"?>` instructions, with paths relative to the declaring file. Imports at the end override earlier keys; optional imports permit missing files, not parse errors. Host `LoadConfiguration("web")` loads only `web.option`, respecting the configured file provider and base path. After adding or removing a plugin entry, call the provider's `Load()` or restart to select it again. Existing entry and imported-file changes use the XML provider's notifications. See the [provider source](src/Configuration/PluginConfigurationProvider.cs).

Initial loading and explicit `Load()` include every loaded descendant plugin. Loading, unloading, full reloads and reloads of associated files notify the host configuration and supply a fresh token for the next change. Replacing, unloading or disposing providers detaches old file subscriptions. A failed full reload clears providers created during that attempt; callers must handle the exception before retrying. Serialize full reloads, plugin tree loading/unloading and disposal. Notifications do not make multiple file updates atomic, add listeners or replace assemblies.

Service scanning runs after the complete plugin tree and its options have loaded. Loaded entry references, the entry assembly and plugin manifests share one deduplication set, so each assembly registers once per host build. `IServiceRegistration.Register` can read descendant plugin configuration. Protocol extensions can therefore register before the host creates its network endpoint snapshot.

Deployment variables and runtime options are different. `--site:daemon` passed to the deployer can select a `*-daemon.plugin` artifact; `site=daemon` passed to the host remains an application context setting and does not select option files. Some plugins use that extra manifest to start workers. Merely installing the primary DLL does not enable such workers.

Do not depend on an incidental ordering of multiple plugins defining the same configuration key. Give environment/site settings a clear owner and verify the effective value without printing secrets.

To diagnose conflicts, read each plugin's effective value through `PluginConfigurationProvider.Providers`. Each entry is a `CompositeConfigurationProvider` containing one XML provider for the selected entry and its imports. For a `FileConfigurationProvider` named `file`, obtain the entry's local path from `file.Source.FileProvider.GetFileInfo(file.Source.Path).PhysicalPath`. This identifies the entry, not the imported file supplying an individual key. Compare values after imports have been applied within each plugin, so normal overrides are not reported as conflicts. Plugins preserves generic entry information; the Web host consumer owns network-key conflict rules and must not rely solely on the final merged value.

`<?zongsoft.tools.binding ...?>` declares exposure for delivery tools. Plugins does not evaluate its templates, generate proxy configuration or start listeners because of this instruction.

### Verification and Operational Risks

1. Check that the intended host content root contains `plugins/Main.plugin` and each feature's manifest.
2. Check plugin names, dependencies, assembly versions, TFM and native runtime architecture.
3. Inspect `plugin.list`/`plugin.tree`, then verify the named service, driver, command or Web endpoint contributed by that plugin.
4. Verify effective configuration and only then connect to an authorized test service.

🚨 Deployment copies and may overwrite files; manifests can also include delete entries. Back up local configuration, inspect the resolved destination and stop the host before replacement. Plugins run with the host's privileges and are not a security sandbox. Loading configuration with file-change support is not a guarantee of safe DLL hot replacement.

See the [host deployment examples](https://github.com/Zongsoft/hosting) and [deployer syntax and options](https://github.com/Zongsoft/tools/tree/main/deployer) for larger multi-plugin applications.

## Real Consumer Plugin: Discussions Modules and Services

### Resolve Public Contracts through the Module Container

[Module.cs](../../discussions/src/Module.cs) declares the assembly's `ApplicationModule(Module.NAME)` and defines the module singleton. These are actual members excerpted from that class:

```csharp
public const string NAME = nameof(Discussions);
public static readonly Module Current = new();

public Module() : base(NAME) { }

private IDataAccess _accessor;
public IDataAccess Accessor => _accessor ??=
	this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
```

The module resolves Core's `IDataAccessProvider` through its own `Services` and selects an accessor by module name, without constructing a database driver. The full module also defines an event registry; this member excerpt is not a complete class file. See [Core](../Zongsoft.Core/README.md) for module-first lookup, shared fallback and ownership.

### Separate Manifests, Configuration and Mappings

- The [business manifest](../../discussions/src/Zongsoft.Discussions.plugin) composes the module, validators, filters and identity extensions.
- The [business options](../../discussions/src/Zongsoft.Discussions.option) provide `general.siteId` and `general.basePath` under `/Discussions`; XML attributes become scalar configuration keys. Repository site values and storage paths are not production configuration.
- The [data mapping](../../discussions/src/Zongsoft.Discussions.mapping) defines real forum entities and relationships; [database scripts](../../discussions/database/) define database structures.
- [ForumService](../../discussions/src/Services/ForumService.cs) registers through `[Service]` and uses Core's data-service base; [ForumController](../../discussions/src/api/Controllers/ForumController.cs) consumes that domain service.

### Trace an Actual Call

For moderator lookup, the controller reads the request's data schema and calls `ForumService.GetModerators`. The service queries the `ForumUser` relationship through `IDataAccess.Select<UserProfile>`. Host deployment and configuration supply the data engine, connection and driver; business objects do not own concrete database clients. Security and site handling require the complete plugin chain, not an isolated query excerpt.

### Verification and Ownership

Use the route templates in the existing [.http requests](../../discussions/docs/http/forum.http), substituting identifiers from your test site and forum. Do not copy identities or service addresses. Check plugin loading, container resolution, configuration, mappings, database and permissions before interpreting results. This section references current source; a historical isolated probe does not certify the complete Discussions business workflow.

Windows terminal automation needs valid console handles. Start from the deployment directory and preserve dependencies such as `runtimes/`. Individual consumers do not dispose shared container services. Restart the isolated host after assembly changes, and clean only resources created for your test.

## Loading and Lifecycle

The host loads manifests, assemblies, the plugin tree and options before registering services and initializing the application context. The workbench visits ordinary extensions, then reads Startup and registers workers when opened. Constructing an instance and starting a worker are separate stages.

BuilderManager coordinates optional OnBuilding callbacks, instance creation, child assembly, optional OnBuilded callbacks, owner appending, and completion state. The owner's builder supplies IAppender. An expose node does not append itself but can receive child objects. Initial construction and cached traversal follow the same appending rules.

BuilderSettings contains only a type hint. IBuilderLifecycle is an optional interface implemented by BuilderBase; its OnBuilding and OnBuilded hooks run around creation and child assembly without repeating on completed cache reuse. Concrete builders set context.AppendToOwner and context.BuildChildren during Build; the manager preserves those decisions with the returned instance. Result is read-only to consumers. Module lookup creates only the instance; normal Auto access completes assembly later. After partial assembly fails with a published instance, subsequent Auto reads rethrow the failure.

ObtainMode.Alway updates the node cache without appending the result and does not replace old references in owner collections. Unloading and host shutdown do not imply object-graph rollback or assembly hot replacement. Avoid startup side effects in constructors. See [Architecture and implementation](implementation.md) for plugin discovery, tree and object relationships, construction diagrams, expressions, configuration, services, host lifecycle and cleanup boundaries.

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

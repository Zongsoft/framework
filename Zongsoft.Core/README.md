# Zongsoft Core

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Core)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Core)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## Overview

_**Z**ongsoft.**C**ore_ is the foundation package of the [_**Z**ongsoft Framework_](https://github.com/Zongsoft/framework). It provides the shared abstractions, base types, utility APIs, and infrastructure building blocks used by the rest of the _**Z**ongsoft_ ecosystem.

The current project targets `net8.0`, `net9.0`, and `net10.0`, uses the root namespace `Zongsoft`, and is packaged as the `Zongsoft.Core` NuGet package. It integrates with the `Microsoft.Extensions.*` stack for dependency injection, configuration, options, hosting abstractions, object pooling, and memory caching.

## Installation

```powershell
dotnet add package Zongsoft.Core
```

## What's Included

The package is intentionally broad: it is the common layer that higher-level _**Z**ongsoft_ packages build on. The main areas are:

- **Common utilities** _(`Zongsoft.Common`)_
  > Conversion helpers, random generation, sequences, predicates, timers, bit vectors, string/type/URI extensions, timestamps, locking helpers, and validation contracts.
- **Collections** _(`Zongsoft.Collections`)_
  > Hierarchical nodes, category trees, parameter bags, synchronized collections, queues, object pools, and collection/dictionary extensions.
- **Components** _(`Zongsoft.Components`)_
  > Command infrastructure, command-line parsing, event exchange, feature pipelines, retry/fallback/breaker/throttle/timeout features, state machines, workers, supervisors, handlers, executors, filters, converters, and identifiers.
- **Configuration** _(`Zongsoft.Configuration`)_
  > Settings and connection settings, configuration binding and recognition, XML configuration providers, model-backed configuration, profile/INI parsing, and options integration with `Microsoft.Extensions.Options`.
- **Data abstractions** _(`Zongsoft.Data`)_
  > Query criteria, conditions, ranges, paging, sorting, operands, model descriptors, data access/service contracts, operation options and events, metadata models, archiving contracts, and transaction primitives.
- **Services** _(`Zongsoft.Services`)_
  > Application context and modules, service registration/discovery helpers, dependency metadata, modular services, service accessors, and distributed-lock abstractions.
- **Caching** _(`Zongsoft.Caching`)_
  > In-memory cache wrappers, eviction/change events, cache scanner, distributed-cache contract, and the `Spooler<T>` batching helper.
- **Communication and messaging** _(`Zongsoft.Communication`, `Zongsoft.Messaging`)_
  > Channel, listener, sender, receiver, requester/responder, transmitter, packetizer, notifier, message queue, producer/consumer, poller, and queue option abstractions.
- **Diagnostics and telemetry** _(`Zongsoft.Diagnostics`)_
  > Logging contracts, console/text/XML loggers and formatters, diagnostic configuration, telemetry meters, metric descriptors, and exporter launcher contracts.
- **IO and hardware** _(`Zongsoft.IO`)_
  > Virtual file-system contracts and local implementation, path parsing, MIME helpers, compression helpers, binary/text reader extensions, and hardware profile models.
- **Security** _(`Zongsoft.Security`)_
  > Claims helpers, credentials, certificates, secret/signature contracts, password utilities, authentication/authorization flows, users, roles, privileges, and privilege evaluators.
- **Serialization** _(`Zongsoft.Serialization`)_
  > Serializer contracts, JSON serializer helpers, serialization options, naming conventions, member attributes, and System.Text.Json converters.
- **Expressions and text** _(`Zongsoft.Expressions`, `Zongsoft.Text`)_
  > Lexer/tokenizer infrastructure, expression evaluator contracts, syntax exceptions, regular-text processing, and template contracts.
- **Reflection** _(`Zongsoft.Reflection`)_
  > High-performance reflection helpers and member-expression parsing/evaluation.
- **Runtime helpers** _(`Zongsoft.Resources`, `Zongsoft.Scheduling`, `Zongsoft.Versioning`, `Zongsoft.Terminals`)_
  > Resource lookup, trigger abstractions, semantic version parsing, and terminal/console command execution.

## Repository Layout

```text
Zongsoft.Core/
  src/        Main library source code.
  test/       xUnit tests for core behaviors.
  samples/    Console samples for MemoryCache, Spooler, Superviser, and EventExchanger.
  benchmark/  BenchmarkDotNet benchmarks for reflection, data model helpers, and Spooler batching.
```

## Build and Test

```powershell
dotnet restore Zongsoft.Core.slnx
dotnet build Zongsoft.Core.slnx -c Release
dotnet test test/Zongsoft.Core.Tests.csproj -c Release
```

The repository also includes a Cake script:

```powershell
dotnet cake build.cake --target=test --edition=Release
```

## Samples

The `samples` directory contains small console applications that exercise real APIs from this package:

- `memorycache`
  > `MemoryCache`, expiration scanning, limit handling, and terminal output.
- `spooler`
  > `Spooler<T>` batching under high write volume.
- `superviser`
  > `Superviser`, `Supervisable`, worker state reporting, and terminal commands.
- `eventexchanger`
  > `EventExchanger` channels and application context integration.

## License

Zongsoft.Core is released under the [LGPL-3.0-or-later](https://github.com/Zongsoft/framework/blob/main/LICENSE) license.

## Services: Using Implementations without Referencing Them

### Application and Module Containers

The plugin host scans deployed assemblies and registers their services. Application code depends on Core contracts or a shared module contract assembly; it does not need to reference each provider's implementation package. Deploying another compatible provider changes composition, not the consumer's business code.

- `ApplicationContext.Current.Services` is the application container, available after host initialization.
- `Module.Current.Services` is the usual application-defined module entry point. Here `Module` is your module class, not a universal Core singleton. [ApplicationModule.Services](src/Services/ApplicationModule.cs) resolves module-specific registrations before falling back to shared application services.
- Constructor injection and `[ServiceDependency]` let the host supply contracts without repeated lookups. Use a module container for module-owned decisions; do not retain HTTP request services in a singleton.

The default attribute scanner registers service implementations as singletons. A module container is not automatic tenant isolation and is not the same as a request scope. Do not dispose a shared service obtained from a container per operation.

### Choosing the Right Lookup

| Need | API | Meaning |
| --- | --- | --- |
| One registered contract | `ResolveRequired<T>()` | Fails when the contract is unavailable |
| All implementations | `ResolveAll<T>()` | Enumerates registered contract implementations |
| A matching implementation | `FindRequired<T>(argument)` | Uses matcher behavior or the contract's Name matching |
| A registered service alias | `ResolveRequired("name")` | Resolves the alias established at registration |
| An instance supplied by a provider | `IServiceProvider<T>.GetService(name)` | Selects a configured named service, not another DI container |
| A configured qualified name | `services.Locate<T>("name@provider")` | Selects a named provider, then its named instance |

Optional forms `Resolve`, `Find` and `Locate` may return null. In applications with multiple providers, make selection explicit; do not let registration order choose the database, queue or cache unintentionally. The [lookup implementation](src/Services/ServiceProviderExtension.cs) defines the matching rules.

### Example: A Real Module's Service Lookup

[Discussions Module](../../discussions/src/Module.cs) declares its module identity in assembly metadata and obtains its data accessor through a Core contract. The following members are an excerpt, not a new module implementation:

```csharp
[assembly: ApplicationModule(Zongsoft.Discussions.Module.NAME)]

public const string NAME = nameof(Discussions);
public static readonly Module Current = new();

private IDataAccess _accessor;
public IDataAccess Accessor => _accessor ??=
	this.Services.ResolveRequired<IDataAccessProvider>().GetAccessor(this.Name);
```

The [plugin manifest](../../discussions/src/Zongsoft.Discussions.plugin) contributes `Module.Current` to `/Workbench/Modules`. The [option file](../../discussions/src/Zongsoft.Discussions.option) owns `/Discussions/General` settings; the host supplies the named database connection. [ThreadService.Posting](../../discussions/src/Services/ThreadService.cs) resolves `PostService` through `this.ServiceProvider.ResolveRequired<PostService>()` rather than constructing it.

This shows module ownership, shared contracts and configuration as separate responsibilities. For expression-provider matching, use the real [Scriban adapter](../externals/scriban/README.md); there is no built-in `Rules:Evaluator` setting or Rules application in Core.

### Example: A Named Cache from a Provider

The connection name below follows the [real Redis cache sample](../externals/redis/samples/distributedcache/Program.cs); the lookup is adapted to a plugin host. A provider is an extra level of indirection: one Redis provider can supply several configured caches. After the Redis plugin and a connection named `Redis` are configured:

```csharp
using Zongsoft.Caching;
using Zongsoft.Services;

var services = ApplicationContext.Current.Services;
IDistributedCache cache = services.Locate<IDistributedCache>("Redis@Redis")
	?? throw new InvalidOperationException("The configured cache is unavailable.");
Console.WriteLine(cache.GetType().Name);
```

The string can be an application option rather than a source-code constant. This example only resolves the service; it does not contact a server. [Redis configuration](../externals/redis/README.md) defines how the named connection is selected, including fallback behavior. Do not assume every provider has a registered alias: check its registration before using `@provider`.

### Registering and Injecting Module Contracts

Provider implementations use `[Service<TContract>]` or `IServiceRegistration`; plugins can also compose objects with service expressions. An assembly's `[ApplicationModule(Zongsoft.Discussions.Module.NAME)]` identifies module ownership for service registration. Modules and their extension nodes must still be contributed by the application's manifest.

`[ServiceDependency]` can inject a contract. A non-empty ServiceName asks an `IServiceProvider<T>` for a named instance; `~` or `.` means the owning module name. Provider selects the module container, while `/` or `*` means the application container. See [ServiceDependencyAttribute](src/Services/ServiceDependencyAttribute.cs).

💡 The `@...` in plugin expressions such as `{service:~@Discussions}` selects a **module container**. The `@Redis` in `ServiceLocator`'s `Redis@Redis` selects a **named service provider**. They are different syntaxes and must not be interchanged.

### Configuration and Lifetime Checklist

Keep provider names, connection names and extension paths in application-owned configuration/assembly metadata. Check that the provider plugin is loaded, that the requested contract is registered, and that its named configuration exists. Missing services should produce an actionable startup error, not silently construct a default implementation.

Use concrete construction for simple Core values or explicitly standalone adapters only where their ownership is intentional. Database connections, queues, expression runtimes and other shared implementations should normally be resolved through the configured provider. Deployment and host setup are explained in [Plugins](../Zongsoft.Plugins/README.md).

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
  > Conversion helpers, random generation, sequences, predicates, timers, bit vectors, string/type/URI extensions, timestamps, locking helpers, and validation contracts. Variable sources include the [`IVariables` contract](docs/variables.md#variable-contract), the case-insensitive in-memory `Variables` dictionary, [live dictionary views through `Variables.Wrap()` / `ToVariables()`](docs/variables.md#dictionary-variable-views), and [live environment variables through `Variables.Environments()`](docs/variables.md#environment-variable-views).
- **Collections** _(`Zongsoft.Collections`)_
  > Hierarchical nodes, category trees, parameter bags, synchronized collections, queues, object pools, and collection/dictionary extensions.
- **Components** _(`Zongsoft.Components`)_
  > Command infrastructure, command-line parsing, [command option variables](docs/variables.md#command-option-variables), event exchange, feature pipelines, retry/fallback/breaker/throttle/timeout features, state machines, workers, supervisors, handlers, executors, filters, converters, and identifiers.
- **Configuration** _(`Zongsoft.Configuration`)_
  > [Settings and connection settings](docs/settings.md), configuration binding and recognition, [XML configuration providers and processing instructions](docs/xml-configuration.md), model-backed configuration, [Profile/INI parsing, directive extensions and variable views](docs/profiles.md), and options integration with `Microsoft.Extensions.Options`.
- **Data abstractions** _(`Zongsoft.Data`)_
  > Query criteria, conditions, ranges, paging, sorting, operands, model descriptors, data access/service contracts, operation options and events, metadata models, archiving contracts, and transaction primitives.
- **Services** _(`Zongsoft.Services`)_
  > Application context and modules, service registration/discovery helpers, dependency metadata, modular services, service accessors, and distributed-lock abstractions.
- **Caching** _(`Zongsoft.Caching`)_
  > In-memory cache wrappers, eviction/change events, cache scanner, distributed-cache contract, and the `Spooler<T>` batching helper.
- **Communication and messaging** _(`Zongsoft.Communication`, `Zongsoft.Messaging`)_
  > [Network address and protocol values](docs/networking.md), channel, listener, sender, receiver, requester/responder, transmitter, packetizer, notifier, message queue, producer/consumer, poller, and queue option abstractions.
- **Diagnostics and telemetry** _(`Zongsoft.Diagnostics`)_
  > Logging contracts, console/text/XML loggers and formatters, diagnostic configuration, telemetry meters, metric descriptors, and exporter launcher contracts.
- **IO and hardware** _(`Zongsoft.IO`)_
  > Virtual file-system contracts and local implementation, path parsing, [Searcher local wildcard search](docs/searcher.md), MIME helpers, compression helpers, binary/text reader extensions, and hardware profile models.
- **Security** _(`Zongsoft.Security`)_
  > Claims helpers, credentials, certificates, secret/signature contracts, password utilities, authentication/authorization flows, users, roles, privileges, and privilege evaluators.
- **Serialization** _(`Zongsoft.Serialization`)_
  > Serializer contracts, JSON serializer helpers, serialization options, naming conventions, member attributes, and System.Text.Json converters.
- **Expressions** _(`Zongsoft.Expressions`)_
  > Lexer/tokenizer infrastructure, expression evaluator contracts, and syntax exceptions.
- **Text and templates** _(`Zongsoft.Text`, `Zongsoft.Text.Templating`)_
  > Regular-text processing, template contracts, and [text template evaluation, member navigation, formatting and diagnostics](docs/expressions.md).
- **Reflection** _(`Zongsoft.Reflection`)_
  > High-performance reflection helpers and member-expression parsing/evaluation.
- **Runtime helpers** _(`Zongsoft.Resources`, `Zongsoft.Scheduling`, `Zongsoft.Versioning`, `Zongsoft.Terminals`)_
  > Resource lookup, trigger abstractions, semantic version parsing, and terminal/console command execution.

## Password Hashing

[`Password`](src/Security/Password.cs) uses PBKDF2 with SHA256 by default. Its `Factor` ranges from `0` to `255` and defaults to `10`. Zero disables additional iterations by performing PBKDF2 once; positive factors map to `Factor × 65,535` iterations. The default factor `10` means 655,350 iterations; factor `127` means 8,322,945 iterations. Execution time depends on the hardware, hash algorithm, and load.

The text format is `algorithm#factor:nonce|value`; `#factor` may be omitted when the factor is zero. `Factor` replaces `Exponent`; hashes generated with earlier iteration mappings must be regenerated.

`Password` implements `ISpanParsable<Password>` and `IParsable<Password>`, with public `Parse` and `TryParse` overloads for strings and character spans. The text format is culture-independent and the interface's format provider is ignored. Null or empty input represents an empty password. Invalid text returns `false` and an empty result from `TryParse`; `Parse` throws `FormatException` instead.

## Service Registration

`ServiceCollectionExtension.Register` discovers `IServiceRegistration` implementations throughout an assembly, including internal classes and private nested classes. It uses an instance supplied by a public static field or property first; otherwise, it invokes a public or non-public parameterless constructor. Open generic types and abstract registrars without a supplied instance are not instantiated. Registration failures propagate to the caller.

Ordinary `[Service]` registrations and option scanning still require externally visible types. A public nested service inside a non-public containing type is not externally visible. This keeps implementation-only services out of automatic registration while allowing private registration helpers.

## Event Dispatch

`EventExchanger` uses `IEventDispatcher` to capture ambient features and a fixed private `IExecutor<EventContext>` for remote delivery. `FeatureProvider.Default` includes `Transaction.Features`, the shared provider of transaction-bound features implementing `IFeature` and `IPredication`, including in standalone use. Applications can extend its `Providers` collection or register `IFeatureProvider` services to add conditions. Channels receive events through `IEventReceiver`. Queue event channels share the topic convention in `Events.GetTopic()` and the incoming message handler from `Events.GetHandler(receiver)`. Default delivery has no policy pipeline; application plugins can explicitly configure Polly policies through `EventDispatcher.Pipeline`. See [event dispatch and ambient features](docs/event-dispatching.md).

## Communication Lifetimes

`ChannelBase.CloseAsync` executes once and shares its result with concurrent callers. Cancellation before entry prevents closing; after entry it only cancels that caller's wait. Closing and closed notifications cannot bypass transport cleanup. Disposal waits for closing before invoking the derived resource cleanup once. A closed channel cannot be reopened; create a new instance. Lifecycle callbacks may request closing but must not synchronously wait for the operation that contains them.

`WorkerBase` coordinates start, stop, pause and resume asynchronously and supports `IAsyncDisposable`. Startup failures propagate and invoke the stop hook to clean partially initialized resources. Stop failures retain `Stopping` and block restart until cleanup succeeds; notification failures do not roll back established state. Synchronous entry points block on the same asynchronous operations.

`ListenerBase<T>` dispatches handler arguments without decoding bytes and does not own injected handlers. An absent handler is a no-op with cancellation checking. Parameterless `HandlerBase<T>` calls pass null parameters; explicitly supplied parameters retain their identity. Both `IPacketizer` interfaces define only `Pack` and `Unpack`: encode without flushing, leave incomplete input untouched, and advance input after successful decoding. Exact frame lengths and decoded-resource release belong to the transport implementation; Net supplies these through its packetizer base class.

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

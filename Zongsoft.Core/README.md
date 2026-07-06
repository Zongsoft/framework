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
  benchmark/  BenchmarkDotNet benchmarks for reflection and data model helpers.
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

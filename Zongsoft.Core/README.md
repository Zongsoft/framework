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

The text format is `algorithm#factor:nonce|value`; `#factor` may be omitted when the factor is zero. Password digests must use the iteration rules above.

`Password` implements `ISpanParsable<Password>` and `IParsable<Password>`, with public `Parse` and `TryParse` overloads for strings and character spans. The text format is culture-independent and the interface's format provider is ignored. Null or empty input represents an empty password. Invalid text returns `false` and an empty result from `TryParse`; `Parse` throws `FormatException` instead.

User password services expose `Passworder.SetAsync` to set or clear a password and two `VerifyAsync` overloads to verify by user identifier or account. Account verification returns the user identifier on success and an empty identifier for a password mismatch; missing or disabled accounts throw `AuthenticationException` with the corresponding reason code. Verification by user identifier returns `false` for a missing user.

A null or empty stored password accepts only null or empty-string input for sign-in and old-password verification. Nonempty input, including whitespace-only strings, fails. Users can supply an empty old password to set a new one. Missing or disabled accounts and malformed stored passwords still fail verification. Use `Zongsoft.Common.Randomizer.GenerateSecret` for random plaintext secrets.

## Service Registration

Use `ServiceCollectionExtension.Register` to register services from an assembly. Public types annotated with `[Service]` participate in automatic registration; implement `IServiceRegistration` when an application needs custom registrations. Registration failures propagate to the caller.

## Event Dispatch

Use `RaiseAsync` to raise events and await their handlers. Bound event handlers must return an awaitable result. `EventExchanger` supports transaction-aware delivery, and applications can add conditions through `IFeatureProvider` or configure policies through `EventDispatcher.Pipeline`. See [event dispatch and ambient features](docs/event-dispatching.md).

## Asynchronous data operations

IDataAccess and IDataService expose asynchronous database methods. Scalar and write operations return ValueTask; queries return IAsyncEnumerable<T>, with cancellation tokens last. Consume queries with await foreach; leaving enumeration early still releases reader resources. Use await using for accessors you own, and leave shared accessors to their owner.

InsertManyAsync, UpsertManyAsync, UpdateManyAsync and accessor ImportAsync accept both in-memory collections and IAsyncEnumerable<T>. Event handlers accept a cancellation token and return ValueTask. Await database and network work inside handlers.

The Getted event for a single-record query exposes an already obtained Result snapshot; reading it never queries again. IPageable.Paginated completes before the first record is yielded, allowing pagination response metadata to be set in time. Template export awaits GetModelAsync to obtain its model.

State machines use `await using` and `await machine.RunAsync(state, description, cancellation)`. Wait for each run before starting the next or disposing the machine; disposal waits for state persistence and transaction completion.

`DataDictionary.GetDictionariesAsync<T>(items, cancellation)` converts an in-memory collection to an asynchronous sequence of data dictionaries. For asynchronous input, use `GetDictionariesAsync<T, TSource>(items, cancellation)`. To prepare each item, pass a `Func<IDataDictionary<T>, CancellationToken, ValueTask>` callback before the token. Enumeration awaits each callback in input order, skips null items, and does not buffer the entire collection.

## Data Validation and Result Processing

Implement `IDataValidator.ValidateAsync` for query conditions and `OnImportAsync`, `OnInsertAsync`, and `OnUpdateAsync` for field values. Field validation returns `DataValidatorResult`: `default` leaves the value unchanged; `HasValue = true` supplies a replacement, including `null`. Use `IDataValueBinder.BindAsync` when the replacement depends on the record being bound.

`Pageable.FilterAsync` and `MapAsync` accept asynchronous callbacks over `IAsyncEnumerable<T>`. They process items when enumerated, preserve paging information, and pass the enumeration cancellation token to the callback. A filter returns `Pageable.FilterResult<T>`: `Accepted = true` emits `Data`, including a replacement or `null`; `default` discards the item.

`Returning.Columns.TryGetValue(name, kind, out column)` looks up a column by case-insensitive name and `ReturningKind`. The overload without `kind`, or a null `kind`, uses the collection's fixed kind, falling back to `Older` when none is fixed. An empty name, incompatible kind, or missing column returns `false` with a default `column`.

`Returning.Rows.Populate(record)` reads the current `IDataRecord` and appends a row; it does not advance or dispose the reader. `Rows.AddRange(rows)` appends the contents of another row collection.

Rows resolve values by case-insensitive name and `ReturningKind`. Later changes to `Columns` do not change existing rows' name mappings. `Row[int]` and `Values` retain the positions of that row's values. `TryGetValue` returns `false` for missing columns and `true` with a `null` value for database nulls. Collections do not support concurrent mutation.

## Transactions

`Transaction` exposes `CommitAsync`, `RollbackAsync`, and `DisposeAsync`. Manage its scope with `await using`; disposal rolls back a transaction that has not committed. Rolling back a nested transaction makes the root transaction rollback-only. Cancellation is checked before accepting a commit; accepted completion and cleanup wait until finished. Repeated calls observe the same completion result, including failures.

`RollbackAsync(exception)` returns normally when rollback and completion notifications succeed. On failure, it throws an `AggregateException` containing the supplied business exception and completion errors. Await rollback in a `catch` block, then decide whether to propagate the original exception with `throw;`:

```csharp
catch(Exception exception)
{
	await transaction.RollbackAsync(exception);
	throw; // Omit this line to handle the business error after successful rollback.
}
```

Batch updates, batch role and user deletion, and state-machine stopping preserve both errors when the operation and transaction completion fail. When only the operation fails, its original exception propagates.

The exception is accepted only when initiating rollback. Once commit or rollback has started, subsequent calls observe the existing result without repeating rollback or adding exceptions. Passing `null` propagates only completion errors. Use `RollbackAsync()` without arguments or pass an explicitly typed cancellation token; `RollbackAsync(default)` is ambiguous between the overloads.

`TransactionContext.Completed` accepts handlers returning `ValueTask` and awaits every handler in subscription order. Notification failures do not change an established transaction outcome. An uncertain outcome, such as a connection failure during commit, remains `Undetermined`; applications must not assume the write rolled back or retry it automatically. Handlers must not await the transaction completion task that includes themselves.

## Application Lifetime

`ApplicationContext` participates in the host's awaited startup and shutdown phases. `Exit()` and `Exit(exitCode)` request host shutdown; they do not terminate the process. `DisposeAsync()` waits for owned worker cleanup before releasing context resources. Application builders expose `BuildAsync()` so initialization failure can await asynchronous disposal. Container-resolved initializers and borrowed service-provider views retain their original owner.

## Terminal Input

`ITerminal.Input` uses `TextReader`, accepting `StreamReader` and `StringReader` directly. `Terminal.Console.Input` provides shared line input with process lifetime: only one caller can wait, and cancellation preserves the read and its result for a subsequent call. Executors do not dispose input. Injected readers follow their own cancellation and ownership contracts. See [Terminal input](docs/terminal-input.md).

`ITerminalExecutor` runs its command loop through three `RunAsync` overloads and returns the exit code when the loop ends. Callers must await the task.

`ITerminal.Abort()` synchronously notifies interruption and returns whether it was handled; it does not stop the executor or process. Hosts own process-signal policy. Core registers no global console signal handlers.

## Logging

Synchronous methods such as `Error(...)`, `Log(...)`, and `Flush()` invoke loggers immediately and return without waiting for asynchronous work. `Logging` does not buffer records or track outstanding operations. Logger failures are observed but are not propagated to synchronous callers; each logger owns its buffering, completion, and failure reporting.

Use awaited asynchronous methods when completion or error propagation is required. `FlushAsync()` delegates to the registered loggers and does not wait for earlier unawaited logging calls. Before releasing loggers, stop producing records, await owned asynchronous logging operations, and await `FlushAsync()`. A logger's processing completion does not necessarily mean persistence.

## Periodic Work

`Common.Timer` executes its constructor callback serially and accepts a `TimeProvider`. `Start()` schedules periodic execution. Startup, stopping, and disposal accept no cancellation tokens. `Stop()` requests internal cancellation without waiting for callbacks or cleanup; `StopAsync()` waits for shutdown. `Dispose()` permanently closes the timer without waiting, while `DisposeAsync()` awaits cleanup even after synchronous disposal. Callers must avoid using business operations after disposal or awaiting completion from within the same run. `Failed` reports run failures, and `Stopped` reports that the callback and resource cleanup have ended. Asynchronous stopping and disposal wait for notifications to return and propagate run and notification failures. See [Asynchronous periodic timers](docs/timers.md).

[`Spooler<T>`](docs/spoolers.md) serializes batch callbacks triggered by a period, a full-buffer write, or an explicit flush. It supports consuming enumeration and inheritance. `PutAsync` admits data; `FlushAsync` waits for an active flush and processes remaining data once. Disposal is asynchronous: `DisposeAsync` stops admission and waits for active flushes and cleanup without draining the buffer. Periodic errors raise `Failed` and propagate from asynchronous disposal; the buffer does not write logs.

Both constructors accept an optional `TimeProvider`. The `Zongsoft.Caching.Spooler` meter exposes admission, queue, flush duration, full-buffer wait, and failure metrics for application-selected export. `FileLogger` supports only asynchronous disposal and releases its resources after active file writes finish.

## Communication Lifetimes

Await `StartAsync`, `StopAsync`, `PauseAsync` and `ResumeAsync` on workers. `Enabled` controls whether startup is allowed; changing it does not stop an active worker. Check `CanPauseAndContinue` before requesting pause or resume. A successfully stopped worker can start again; a disposed worker cannot.

Use `await using` or await `DisposeAsync` for owned workers, listeners, queues, pollers and distributed locks. Container-owned services are released by their container. Await channel closure through `CloseAsync`; a closed channel cannot be reopened. Lifecycle callbacks must not wait for their own shutdown or disposal.

Read request responses with `await foreach` over `IRequestToken.GetResponsesAsync`. Await message confirmation through `Message.AcknowledgeAsync`; a completed handler does not automatically imply confirmation.

Worker commands accept `--timeout` to limit waiting. The default is five seconds, non-positive values use five seconds, and the maximum is thirty seconds. A timeout raises `TimeoutException` while the worker operation continues.

The Windows `shell` command waits for process exit and redirected output. Its timeout defaults to one second; non-positive values use thirty seconds. A timeout returns `-1`, and caller cancellation propagates. Neither terminates the launched process.

## Repository Layout

```text
Zongsoft.Core/
  src/               Main library source code.
  test/unit/         xUnit unit tests.
  test/integration/  Separate terminal input and process integration tests.
  samples/           Console samples for MemoryCache, Spooler, Superviser, and EventExchanger.
  benchmark/         BenchmarkDotNet benchmarks for reflection, data model helpers, and Spooler batching.
```

## Build and Test

```powershell
dotnet restore Zongsoft.Core.slnx
dotnet build Zongsoft.Core.slnx -c Release
dotnet test test/unit/Zongsoft.Core.Tests.csproj -c Release
```

The repository also includes a Cake script:

```powershell
dotnet cake build.cake --target=test --edition=Release
```

The main solution and the default Cake target run only `test/unit`. Run [integration tests](test/integration/README.md) explicitly:

```powershell
dotnet test test/integration/Zongsoft.Core.Tests.slnx -c Release -p:GeneratePackageOnBuild=false
# Or: dotnet cake build.cake --target=integration-test --edition=Release
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

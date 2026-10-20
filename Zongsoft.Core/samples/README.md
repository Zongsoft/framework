# Zongsoft.Core Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [eventexchanger](eventexchanger) | Exercises `EventExchanger`, event channels, concurrent publishing, and dispatch statistics. |
| [memorycache](memorycache) | Demonstrates cache limits, expiration, eviction notifications, and `MemoryCacheScanner`. |
| [spooler](spooler) | Exercises `Spooler<T>` batching with repeated input values under parallel load. |
| [superviser](superviser) | Demonstrates supervised object lifecycle, inactivity, failure handling, and manual removal. |

All projects target .NET 10. Build the complete sample solution from the repository root:

```shell
dotnet build Zongsoft.Core/samples/samples.slnx
```

## Event Exchanger

```shell
dotnet run --project Zongsoft.Core/samples/eventexchanger/Zongsoft.Samples.EventExchanger.csproj -- --local
```

`--local` runs local handlers without creating a message queue. Without this option, start a local ZeroMQ broker before running the sample. See the [event exchange guide](eventexchanger/README.md) for build prerequisites and remote delivery between two processes.

The program accepts the following interactive input:

- `start` or `restart` starts the exchanger; `stop` stops it.
- `info` lists the current event channels; `reset` resets sample counters.
- `<quantity>` raises one event containing the specified number of metrics.
- `<quantity>/<rounds>` or `<quantity>@<rounds>` raises the specified number of events concurrently, each containing `<quantity>` metrics.
- `clear` clears the terminal and `exit` quits.

Example sequence:

```text
start
1000
1000/10
info
stop
```

## Memory Cache

```shell
dotnet run --project Zongsoft.Core/samples/memorycache/Zongsoft.Samples.MemoryCache.csproj
```

Any text other than a control command is inserted into the cache with a 30-second expiration. The cache has a five-entry limit and a one-second scan frequency. Use `count` to show the current count, `start` or `restart` to start the scanner, `stop` to stop it, and `exit` to quit. Add more than five values to observe the `Limited` event; keep the scanner running to observe `Evicted` events.

## Spooler

```shell
dotnet run --project Zongsoft.Core/samples/spooler/Zongsoft.Samples.Spooler.csproj
```

Use `info` to display or update defaults:

```text
info --period:100 --limit:100000 --count:1000000 --collision:0
```

Run a parallel spooling test with positional arguments or named options:

```text
spool 100000 1000
spool --count:100000 --collision:1000
```

`count` is the number of values produced. A positive `collision` value limits random values to that range and therefore creates duplicate values; zero uses unrestricted random values. The sample reports handled counts, batching behavior, and elapsed time.

## Superviser

```shell
dotnet run --project Zongsoft.Core/samples/superviser/Zongsoft.Samples.Superviser.csproj
```

Use `create`, `open`, `close`, `pause`, `resume`, `error`, `reset`, and `info` to exercise object states. See the [complete superviser scenarios](superviser/README.md) for command sequences covering inactivity, permitted failures, persistent supervision, and manual removal.

## Background and Safe Experiment Size

An event exchanger dispatches registered events through channels; a spooler groups pending items into batches; a superviser observes activity and failures; cache scanning triggers expiration observation. These mechanisms solve different lifecycle problems and are not interchangeable durable queues.

The event exchange sample also supports ZeroMQ delivery between processes; `--local` keeps event handling within the current process. The other samples use local in-process objects. Start with a few dozen events/items before the larger examples above. Their timing output is an interactive observation, not a reproducible performance benchmark.

## Code and Cleanup

Each project has its own Program.cs: [events](eventexchanger/Program.cs), [cache](memorycache/Program.cs), [spooler](spooler/Program.cs), [superviser](superviser/Program.cs). Read initialization and shutdown together when adapting a sample.

Use `stop` where offered, then `exit`; the samples dispose their in-process workers and timers. There are no application databases to erase. Counters and cache state reset when a process ends.

💡 A delayed eviction callback is not necessarily a failed expiration: timer scheduling and explicit scanning affect when the sample reports it. Do not turn console timing into a strict unit-test assertion.

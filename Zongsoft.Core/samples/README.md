# Zongsoft.Core Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Projects

| Project | Purpose |
| --- | --- |
| [eventexchanger](eventexchanger) | Exercises `EventExchanger`, event channels, concurrent publishing, and dispatch statistics. |
| [memorycache](memorycache) | Demonstrates cache limits, expiration, eviction notifications, and `MemoryCacheScanner`. |
| [spooler](spooler) | Exercises `Spooler<T>` batching and key-collision behavior under parallel load. |
| [superviser](superviser) | Demonstrates supervised object lifecycle, inactivity, failure handling, and manual removal. |

All projects target .NET 10. Build the complete sample solution from the repository root:

```shell
dotnet build Zongsoft.Core/samples/samples.slnx
```

## Event Exchanger

```shell
dotnet run --project Zongsoft.Core/samples/eventexchanger/Zongsoft.Samples.EventExchanger.csproj
```

The program accepts the following interactive input:

- `start` or `restart` starts the exchanger; `stop` stops it.
- `info` lists the current event channels; `reset` resets sample counters.
- `<quantity>` raises one round containing the specified number of events.
- `<quantity>/<rounds>` or `<quantity>@<rounds>` raises multiple rounds.
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

`count` is the number of values produced. A positive `collision` value limits random values to that range and therefore creates duplicate keys; zero uses unrestricted random values. The sample reports handled counts, batching behavior, and elapsed time.

## Superviser

```shell
dotnet run --project Zongsoft.Core/samples/superviser/Zongsoft.Samples.Superviser.csproj
```

Use `create`, `open`, `close`, `pause`, `resume`, `error`, `reset`, and `info` to exercise object states. See the [complete superviser scenarios](superviser/README.md) for command sequences covering inactivity, permitted failures, persistent supervision, and manual removal.

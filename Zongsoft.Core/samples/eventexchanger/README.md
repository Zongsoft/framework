# Event exchange sample

[English](README.md) | [简体中文](README.zh-Hans.md)

This sample provides interactive local event publishing and remote event delivery through ZeroMQ. See [event dispatching and ambient features](../../docs/event-dispatching.md) for the contracts and boundaries.

## Interactive use

Run from the framework directory:

```powershell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0 -p:GeneratePackageOnBuild=false
dotnet build messaging/zero/src/Zongsoft.Messaging.ZeroMQ.csproj -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project Zongsoft.Core/samples/eventexchanger/Zongsoft.Samples.EventExchanger.csproj -- --local
```

`--local` registers only the local handler and does not create a queue. Enter `3/5` to publish five events concurrently, each containing three metrics. Elapsed time is reported after all handlers finish. `start`, `stop`, and `restart` control the exchanger; `reset` resets counters. `exit` or end of input drains the exchanger and releases resources owned by the sample.

Without `--local`, the sample connects to the local ZeroMQ discovery port `7969`. Start a local broker, run the sample in two terminals, and enter `start` in each. Each event runs locally at its source and is delivered to other subscribers. The channel borrows its queue; the sample disposes the queue it created when exiting.

# Zongsoft.Externals.Polly Extension Plugin Library Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Purpose and Startup

This interactive .NET 10 program assembles resilience features around a simulated operation; it needs no database or remote service. [Program.cs](Program.cs) registers the commands, builds a pipeline for `execute`, and simulates delay or failure. For application-side plugin composition, see the [library guide](../README.md).

From the repository root, build Core first because this sample's Debug configuration references its output DLL, then start the sample in an interactive terminal:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project externals/polly/samples/Zongsoft.Externals.Polly.Samples.csproj -c Debug
```

Enter the commands below at the sample prompt, not at the operating-system shell. `info` displays the current features; `reset` clears them. The sample handles simulated errors and prints `Caught:`; this is expected for an unhandled final failure.

💡 Concurrent execution uses `Parallel.For` with an asynchronous callback. The prompt can return before all work completes. Event ordering and counts depend on scheduling, especially with millisecond windows; these examples are observations, not deterministic assertions or performance benchmarks. Wait for output to settle before resetting a pipeline. Some rejection handlers also call `Console.Beep`, which depends on the platform.

💡 **Note:** the _(**F**allback)_ strategy should be the outermost strategy, meaning it should be added to the execution pipeline first.

## Retry

Run the following commands in order:

> - `reset | retry`
> - `exec --throw`

You should observe the default `3` `OnRetry` retries.

## Timeout

Run the following commands in order:

> - `reset | timeout 10ms`
> - `exec --delay:100ms`

You should observe `1` `OnTimeout` timeout.

## Circuit Breaker

Run the following commands in order:

> - `reset | breaker`
> - `exec --round:100 --concurrency --throw`

Observe `Opened` events when failures meet the configured circuit-breaking threshold; their count depends on scheduling.

## Fallback

Run the following commands in order:

> - `reset | fallback`
> - `exec --throw`

You should observe `1` `OnFallback` fallback.

### Timeout with Fallback

Run the following commands in order:

> - `reset | fallback | timeout 10ms`
> - `exec --delay:100ms`

You should observe `1` `OnTimeout` timeout followed by `OnFallback` fallback.

## Rate Limiting and Throttling

> 💡 Tip: if you do not want a `ThrottleException` to be thrown, enable the `--handled` option on the `throttle` command.

### Concurrency Limit

Run the following commands in order:

> - `reset | throttle --handled`
> - `exec --round:5 --concurrency`

Observe `OnRejected` events when concurrent work exceeds the configured capacity; the exact count is not fixed.

### Token Bucket Limit

Run the following commands in order:

> - `reset | throttle token --permit:1 --queue:0 --value:1 --period:1ms --handled`
> - `exec --round:5 --concurrency`

Observe `OnRejected` events when no token is available; the exact count depends on token replenishment and scheduling.

### Fixed Window Limit

Run the following commands in order:

> - `reset | throttle fixed --permit:1 --queue:0 --window:1ms --handled`
> - `exec --round:5 --delay:1ms --concurrency`

Observe `OnRejected` events when the current window has no remaining capacity; the exact count is not fixed.

### Sliding Window Limit

Run the following commands in order:

> - `reset | throttle sliding --permit:1 --queue:0 --window:1ms --segments:1 --handled`
> - `exec --round:5 --delay:1ms --concurrency`

Observe `OnRejected` events when the sliding window has no remaining capacity; the exact count is not fixed.

## Exit and Cleanup

After pending output finishes, use `exit` and confirm the terminal's exit prompt. Features and simulated state are process-local; the sample creates no database records or application data files. Do not interpret stopping the process as proof that a real application's in-flight work has drained.

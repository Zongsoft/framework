# Zongsoft.Externals.Polly Extension Plugin Library Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

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

You should observe `1` to `3` `Opened` circuit-breaker events.

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

You should observe about `3` to `4` `OnRejected` events.

### Token Bucket Limit

Run the following commands in order:

> - `reset | throttle token --permit:1 --queue:0 --value:1 --period:1ms --handled`
> - `exec --round:5 --concurrency`

You should observe about `3` to `4` `OnRejected` events.

### Fixed Window Limit

Run the following commands in order:

> - `reset | throttle fixed --permit:1 --queue:0 --window:1ms --handled`
> - `exec --round:5 --delay:1ms --concurrency`

You should observe `4` `OnRejected` events.

### Sliding Window Limit

Run the following commands in order:

> - `reset | throttle sliding --permit:1 --queue:0 --window:1ms --segments:1 --handled`
> - `exec --round:5 --delay:1ms --concurrency`

You should observe `4` `OnRejected` events.

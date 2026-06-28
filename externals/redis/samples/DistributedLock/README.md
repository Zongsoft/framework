# Redis Distributed Lock

This sample validates the Redis distributed lock implementation in `RedisService.DistributedLock.cs`.

The sample contains two executable projects:

- `master`: resets Redis state, starts slaver processes for automatic tests, and reports counters.
- `slaver`: competes for the distributed lock and records whether multiple workers enter the critical section at the same time.

Both projects use Zongsoft's command executor and terminal output APIs, following the style of other samples in this repository. Command options are parsed by `Zongsoft.Components.CommandLine`; use `--name:value` or `--name=value` for option values.

## References

- Redis official distributed-lock pattern: https://redis.io/docs/latest/develop/clients/patterns/distributed-locks/
  Acquire with `SET key value NX PX ttl`; release with a Lua script that deletes only when the stored token matches the caller token.
- RedLock.net: https://github.com/samcook/redlock.net
- redlock-rb: https://github.com/leandromoreira/redlock-rb
- node-redlock: https://github.com/mike-marcacci/node-redlock

## Scenarios

- `mutex`: critical-section duration is shorter than the lock expiry. The expected result is zero overlapping entries.
- `expiry`: critical-section duration intentionally exceeds the lock expiry. The expected result is at least one overlapping entry, proving the sample can expose TTL-window failures.

## Build

Build both projects first:

```pwsh
dotnet build externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug
dotnet build externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug
```

The default Redis endpoint is `127.0.0.1:6379`, database `15`, and password from `REDIS_PASSWORD`, falling back to `xxxxxx`.

```pwsh
$env:REDIS_PASSWORD = "xxxxxx"
```

## Automatic Test

The master can start multiple slaver processes automatically.

Normal mutex test:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- run --workers:8 --iterations:80
```

Expiry-risk test:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- run --scenario:expiry --workers:8 --iterations:12
```

## Manual Test

Use the same `run-id` in all commands. The namespace will be `DistributedLock:<run-id>:<scenario>`.

1. Reset state from a master terminal:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-001 --scenario:mutex
```

2. Start several slaver terminals. Use a different `worker-id` in each terminal:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:1 --iterations:40
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:2 --iterations:40
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:3 --iterations:40
```

3. After the slavers exit, report from master. `expected` is `worker-count * iterations`:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-001 --scenario:mutex --expected:120
```

For the expiry-risk scenario, use `--scenario:expiry` on reset, slaver, and report commands. The report expects violations:

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-expiry --scenario:expiry
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:1 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:2 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-expiry --scenario:expiry --expected:16
```

You can override the full Redis connection string on any command:

```pwsh
--connection:"server=127.0.0.1;port=6379;password=xxxxxx;database=15;"
```

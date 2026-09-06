# Etcd Distributed Lock Sample

[English](README.md) | [简体中文](README.zh-Hans.md)

## Purpose and Concepts

Two independent processes, [master](master/README.md) and [slaver](slaver/README.md), compete for the same `worker` lock and increment a shared counter. The names label the processes; neither is a permanently elected leader.

A **lease** expires ownership if it is not renewed. A **fencing token** is a monotonically increasing acquisition revision that a downstream resource must check to reject stale owners. Holding a lease alone cannot stop an isolated process from continuing its work. See the [Etcd adapter](../../README.md) for the public contract.

## Prerequisites

Use .NET 10 SDK and a disposable etcd v3 instance on `127.0.0.1:2379`. Both programs use namespace `samples:distributed-lock`; do not share it with production. The Debug projects reference the locally built Core DLL.

From the framework root, prepare Core:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
```

## Run

Run these in separate terminals from the framework root:

```shell
dotnet run --project externals/etcd/samples/distributedlock/master
dotnet run --project externals/etcd/samples/distributedlock/slaver
```

Each program accepts an optional first argument containing the connection settings. Do not pass credentials on a shared command line.

## Key Code and Expected Result

Each process calls `AcquireAsync`, then `EnterAsync` to wait until ownership is held. It increments `counter`, prints the process name, fencing token and counter, and releases the lock via `await using`. Expiry is 5 seconds and renewal is requested every 2 seconds. Master waits 750 ms per iteration; slaver waits 1 second. Each runs 10 iterations.

Output order is nondeterministic. Counter values persist between runs; fencing tokens need not be consecutive. The sample **prints** fencing tokens but does not implement a downstream fenced write.

## Cleanup and Troubleshooting

Normal scope exit releases the lock; unexpected process termination relies on lease expiration. After both processes stop, remove only their keys under `samples:distributed-lock:` if a clean rerun is needed. The counter does not expire automatically. Do not clear the whole etcd database.

💡 Connection refused indicates a service/port problem; prolonged contention can indicate another sample still using the namespace. Do not shorten the lease below realistic scheduling and network delays.

# Etcd Lock master Process

[English](README.md) | [简体中文](README.zh-Hans.md)

## Purpose

This is the master participant in the [two-process lock example](../README.md). It uses the same lock and counter as its peer; it is not a separate lock implementation or a permanent leader/follower role.

## Prerequisites and Run

Use .NET 10, a locally built Debug Core library and a disposable etcd v3 instance. Follow the parent guide to prepare Core. From this directory:

```shell
dotnet run -- "server=127.0.0.1;port=2379"
```

Start the other process in another terminal to observe contention. Never use production credentials or a shared business namespace.

## Code and Expected Result

[Program.cs](Program.cs) constructs `EtcdService`, sets namespace `samples:distributed-lock`, acquires `worker` with 5-second expiry and 2-second renewal, then calls `EnterAsync`. It increases `counter`, prints `MASTER fence=..., counter=...`, and waits 750 ms before scope disposal. There are 10 iterations.

The peer may print several lines before this process acquires the lock. Counter values survive restarts; token gaps are normal. The downstream write is not fenced by this sample.

## Cleanup

Normal completion disposes each lock and the client. After both processes exit, follow the [parent cleanup instructions](../README.md); remove only sample-owned keys. A killed process leaves its lease to expire.

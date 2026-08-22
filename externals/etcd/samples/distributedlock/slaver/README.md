# Distributed lock slaver sample

[English](README.md) | [简体中文](README.zh-Hans.md)

This second process competes with the master for the same `worker` lock and protected counter, demonstrating cross-process exclusion and increasing fencing tokens.

```shell
dotnet run --project Zongsoft.Externals.Etcd.DistributedLock.Slaver.csproj
```

Pass an etcd connection string as the first argument to override `server=127.0.0.1;port=2379`.

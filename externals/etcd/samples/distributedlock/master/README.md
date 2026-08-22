# Distributed lock master sample

[English](README.md) | [简体中文](README.zh-Hans.md)

This process contends for `worker`, increments the protected counter after entering the lock, and prints its fencing token. Run it together with the slaver sample.

```shell
dotnet run --project Zongsoft.Externals.Etcd.DistributedLock.Master.csproj
```

Pass an etcd connection string as the first argument to override `server=127.0.0.1;port=2379`.

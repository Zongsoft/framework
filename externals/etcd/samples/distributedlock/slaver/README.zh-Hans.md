# 分布式锁 Slaver 示例

[English](README.md) | [简体中文](README.zh-Hans.md)

该进程与 master 竞争相同的 `worker` 分布式锁和受保护计数器，用于演示跨进程互斥与单调递增的栅栏令牌。

```shell
dotnet run --project Zongsoft.Externals.Etcd.DistributedLock.Slaver.csproj
```

可通过第一个命令行参数传入 etcd 连接字符串；默认值为 `server=127.0.0.1;port=2379`。

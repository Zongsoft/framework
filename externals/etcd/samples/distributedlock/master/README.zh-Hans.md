# 分布式锁 Master 示例

[English](README.md) | [简体中文](README.zh-Hans.md)

该进程竞争名为 `worker` 的分布式锁，进入临界区后递增受保护的计数器，并输出对应的栅栏令牌。请与 slaver 示例同时运行。

```shell
dotnet run --project Zongsoft.Externals.Etcd.DistributedLock.Master.csproj
```

可通过第一个命令行参数传入 etcd 连接字符串；默认值为 `server=127.0.0.1;port=2379`。

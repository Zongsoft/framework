# Etcd 分布式锁样例

[English](README.md) | [简体中文](README.zh-Hans.md)

## 用途与基础概念

[master](master/README.zh-Hans.md) 与 [slaver](slaver/README.zh-Hans.md) 两个独立进程竞争同一个 `worker` 锁并递增共享计数器。名称仅用来区分进程，不表示固定选举出的主节点。

**租约**在未续期时使所有权过期。**栅栏令牌**是单调递增的获取修订号，下游资源必须检查它才能拒绝旧持有者。只有租约并不能阻止网络隔离进程继续工作。公开契约见 [Etcd 适配器](../../README.zh-Hans.md)。

## 前置条件

需要 .NET 10 SDK，以及位于 `127.0.0.1:2379` 的临时 etcd v3 实例。两个程序共用 `samples:distributed-lock` 命名空间，勿与生产混用。Debug 项目引用本地构建的 Core DLL。

在 framework 根目录先准备 Core：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
```

## 运行步骤

从 framework 根目录在两个终端分别运行：

```shell
dotnet run --project externals/etcd/samples/distributedlock/master
dotnet run --project externals/etcd/samples/distributedlock/slaver
```

程序都接受第一个可选参数作为连接设置。不要在共享命令行中传入凭据。

## 关键代码与预期结果

每个进程先调用 `AcquireAsync`，再用 `EnterAsync` 等待获得所有权；递增 `counter`，打印进程名、栅栏令牌与计数器，通过 `await using` 释放锁。租约有效期为 5 秒，每 2 秒请求续期。Master 每轮等待 750 毫秒，slaver 等待 1 秒，各运行 10 轮。

输出交错顺序不确定，计数器跨运行保留，栅栏令牌不必连续。样例只**打印**栅栏令牌，没有实现下游受栅栏保护的写入。

## 清理与排障

正常离开作用域会释放锁；进程异常退出依靠租约过期。两个进程停止后，如需从头重跑，仅清理 `samples:distributed-lock:` 下属于样例的键。计数器不会自动过期，不要清空整个 etcd 数据库。

💡 连接被拒绝通常是服务或端口问题；长时间竞争可能是另一个样例仍在使用同一命名空间。租约不能短于实际调度和网络延迟。

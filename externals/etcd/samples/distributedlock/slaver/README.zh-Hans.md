# Etcd 锁 slaver 进程

[English](README.md) | [简体中文](README.zh-Hans.md)

## 用途

这是[双进程锁样例](../README.zh-Hans.md)中的 slaver 参与者，与另一个进程共用锁及计数器，不是另一套锁实现，也不表示固定主从角色。

## 前置条件与运行

需要 .NET 10、本地构建的 Debug Core 类库及临时 etcd v3 实例。按上级指南准备 Core 后，在本目录执行：

```shell
dotnet run -- "server=127.0.0.1;port=2379"
```

在另一终端启动另一个进程以观察竞争。不要使用生产凭据或共用业务命名空间。

## 关键代码与预期结果

[Program.cs](Program.cs) 创建 `EtcdService`，设置 `samples:distributed-lock` 命名空间，以 5 秒有效期及 2 秒续期间隔获取 `worker`，再调用 `EnterAsync`。随后递增 `counter`，打印 `SLAVER fence=..., counter=...`，等待 1 s 后离开作用域释放锁，共运行 10 轮。

另一个进程可能先输出多行，本进程才获得锁。计数器跨重启保留，令牌有间隔是正常现象。样例没有对下游写入实施栅栏校验。

## 清理方式

正常完成会释放各次锁和客户端。两个进程退出后按[上级清理说明](../README.zh-Hans.md)只删除样例键；被强制结束的进程所持租约等待过期。

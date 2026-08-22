# Zongsoft.Externals.Etcd 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Etcd)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Etcd)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) | [简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Etcd` 将 [etcd](https://etcd.io/) 集成到 Zongsoft 基础设施抽象中，既可作为 Zongsoft 插件加载，也可由应用直接引用。

## 主要功能

- 具名 etcd 服务，以及按命名空间隔离的 UTF-8 键值操作；
- 基于 `ISequence` 和 `ISequenceBase` 的原子整数、浮点序列；
- 基于租约的分布式锁，支持所有权令牌、手动/自动续期和单调递增的栅栏令牌；
- 获取、设置、查找、计数、删除、序列和锁等命令树操作。

## 连接设置

加载 `Zongsoft.Externals.Etcd.plugin`，并配置 `/Externals/Etcd/ConnectionSettings`：

```xml
<connectionSettings>
	<connectionSetting connectionSetting.name="local" driver="etcd"
	                   value="server=127.0.0.1;port=2379;timeout=10s" />
</connectionSettings>
```

`server` 也可填写逗号分隔的端点列表；`username` 和 `password` 用于启用 etcd 身份验证。请在首次操作前设置 `Namespace` 以隔离逻辑键，服务激活后不能再改变该值。

## 直接使用

```csharp
using Zongsoft.Externals.Etcd;
using Zongsoft.Services.Distributing;

using var etcd = new EtcdService("local", "server=127.0.0.1;port=2379")
{
	Namespace = "orders"
};

await etcd.SetValueAsync("status", "ready");
var orderNumber = await etcd.IncreaseAsync("number", seed: 1000);

var options = new DistributedLockOptions(TimeSpan.FromSeconds(10))
{
	RenewalInterval = TimeSpan.FromSeconds(3)
};

await using var locker = await etcd.AcquireAsync("writer", options);
await locker.EnterAsync();
Console.WriteLine($"栅栏令牌：{locker.FencingToken}");
```

`AcquireAsync` 不会等待竞争锁，竞争失败时返回未持有对象；需要等待锁时请调用 `EnterAsync`。只有设置 `RenewalInterval` 才会自动续期，受保护的存储还应拒绝过期的栅栏令牌。

etcd 租约以整秒为粒度，小于一秒的正数有效期会向上取整为一秒。

## 本机 etcd 与示例

Podman 配置位于 `D:\Zongsoft\hosting\zongsoft.pod-etcd.yaml`，也可在 hosting 的 Pod 启停脚本中选择 `etcd`。

- [序列示例](samples/sequence)
- [分布式锁示例](samples/distributedlock)

## 参考资料

- [dotnet-etcd 文档](https://github.com/shubhamranjan/dotnet-etcd/tree/main/docs)
- [etcd 官方文档](https://etcd.io/docs)
- [etcd 中文文档](https://github.com/FlamingTree/etcd-doc-zh/tree/master/documentation)

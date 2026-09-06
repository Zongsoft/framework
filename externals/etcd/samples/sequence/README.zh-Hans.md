# Etcd 序号样例

[English](README.md) | [简体中文](README.zh-Hans.md)

## 用途与基础概念

本可执行程序演示通过 [EtcdService](../../README.zh-Hans.md) 实现整数与浮点数的分布式递增。适配器用比较交换事务解决并发修改，序号是存储中的数据，而不是进程内计数器。

种子只在键不存在时使用。首次结果为 `seed + interval`，不是种子本身。

## 前置条件与运行

需要 .NET 10 与临时 etcd v3 端点。Debug 构建依赖本地 Core DLL。在 framework 根目录执行：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet run --project externals/etcd/samples/sequence -- "server=127.0.0.1;port=2379"
```

第一个位置参数覆盖默认连接设置；程序始终选择 `samples:sequence` 命名空间。

## 关键代码

```csharp
using Zongsoft.Externals.Etcd;

using var service = new EtcdService("sample", "server=127.0.0.1;port=2379")
{
	Namespace = "samples:sequence",
};

var number = await service.IncreaseAsync("orders", seed: 1000);
var fraction = await service.IncreaseAsync("score", 0.25, 1.5);
Console.WriteLine(number);
Console.WriteLine(fraction);
```

## 预期结果与清理

键不存在时结果为 1001 和 1.75，第二次运行得到 1002 和 2.0；其它进程或既存数据会改变结果。浮点操作遵循 double 精度，不适合要求精确的小数金额。

释放客户端不会删除存储值。所有样例进程停止后，仅删除 `samples:sequence:orders` 与 `samples:sequence:score` 即可重置实验。

🚨 不要重置用于业务标识的序号：重复使用旧值可能导致身份重复。

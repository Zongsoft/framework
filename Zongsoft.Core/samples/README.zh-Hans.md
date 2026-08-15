# Zongsoft.Core 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [eventexchanger](eventexchanger) | 演示 `EventExchanger`、事件通道、并发发布和分派统计。 |
| [memorycache](memorycache) | 演示缓存容量限制、过期、逐出通知和 `MemoryCacheScanner`。 |
| [spooler](spooler) | 在并行负载下演示 `Spooler<T>` 批处理及键冲突行为。 |
| [superviser](superviser) | 演示受督对象生命周期、失活、失败处理和手动移除。 |

所有项目均面向 .NET 10。在仓库根目录构建完整范例解决方案：

```shell
dotnet build Zongsoft.Core/samples/samples.slnx
```

## 事件交换器

```shell
dotnet run --project Zongsoft.Core/samples/eventexchanger/Zongsoft.Samples.EventExchanger.csproj
```

程序接受以下交互输入：

- `start` 或 `restart` 启动交换器，`stop` 停止交换器；
- `info` 列出当前事件通道，`reset` 重置范例计数；
- `<数量>` 触发一轮指定数量的事件；
- `<数量>/<轮数>` 或 `<数量>@<轮数>` 触发多轮事件；
- `clear` 清空终端，`exit` 退出。

示例执行顺序：

```text
start
1000
1000/10
info
stop
```

## 内存缓存

```shell
dotnet run --project Zongsoft.Core/samples/memorycache/Zongsoft.Samples.MemoryCache.csproj
```

除控制命令外的任意文本都会以 30 秒过期时间写入缓存。缓存容量上限为 5，扫描频率为 1 秒。使用 `count` 显示当前数量，使用 `start` 或 `restart` 启动扫描器，使用 `stop` 停止扫描器，使用 `exit` 退出。连续写入超过 5 个值可观察 `Limited` 事件；保持扫描器运行可观察 `Evicted` 事件。

## 缓冲器

```shell
dotnet run --project Zongsoft.Core/samples/spooler/Zongsoft.Samples.Spooler.csproj
```

使用 `info` 显示或更新默认参数：

```text
info --period:100 --limit:100000 --count:1000000 --collision:0
```

通过位置参数或具名选项运行并行缓冲测试：

```text
spool 100000 1000
spool --count:100000 --collision:1000
```

`count` 表示产生的值数量。`collision` 为正数时将随机值限制在指定范围内，从而产生重复键；为零时使用不受限的随机值。范例会报告处理数量、批处理情况和耗时。

## 督管器

```shell
dotnet run --project Zongsoft.Core/samples/superviser/Zongsoft.Samples.Superviser.csproj
```

使用 `create`、`open`、`close`、`pause`、`resume`、`error`、`reset` 和 `info` 演示对象状态。有关失活、允许失败、持续督管和手动移除的完整命令顺序，请参阅[督管器范例说明](superviser/README.zh-Hans.md)。

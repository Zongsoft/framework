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
<options>
	<option path="/Externals/Etcd">
		<connectionSettings>
			<connectionSetting connectionSetting.name="local" driver="etcd"
			                   value="server=127.0.0.1;port=2379;timeout=10s" />
		</connectionSettings>
	</option>
</options>
```

`server` 也可填写逗号分隔的端点列表；`username` 和 `password` 用于启用 etcd 身份验证。请在首次操作前设置 `Namespace` 以隔离逻辑键，服务激活后不能再改变该值。

## 从插件服务容器使用

消费序号或锁的业务模块只需引用 `Zongsoft.Core`。以下将[真实序列样例](samples/sequence/Program.cs)改为具有超时、唯一过期键的宿主调用，运行在已加载 Etcd 插件及上述配置的宿主命令或应用服务中，并假设该容器只注册了一个序号提供者：

```csharp
using Zongsoft.Common;
using Zongsoft.Services;

var provider = ApplicationContext.Current.Services
	.ResolveRequired<Zongsoft.Services.IServiceProvider<ISequence>>();
var sequence = provider.GetService("local")
	?? throw new InvalidOperationException("Sequence service not found.");

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var key = $"docs:sequence:{Guid.NewGuid():N}";
var value = await sequence.IncreaseAsync(key, seed: 1000,
	expiry: TimeSpan.FromMinutes(1), cancellation: cancellation.Token);
Console.WriteLine(value);
```

新键首次返回 `1001`，测试键约一分钟后过期。按名解析的是连接设置 `local`，不是插件名；调用方不要释放容器共享的提供者或服务。模块内可改用 `Module.Current.Services`。

💡 当前 Etcd 提供者没有注册名为 `Etcd` 的服务别名，也没有实现按该名字匹配；不能照搬 Redis 的 `local@Redis` 写法为 `local@Etcd`。若同一容器同时部署多个 `IServiceProvider<ISequence>`，由应用组合层明确选择/注入所需提供者，不依赖枚举顺序。

序号和锁解决不同问题：序号保证原子分配，但不保证业务事务提交或无间断编号；锁用租约限制持有时间，进程暂停或失联后仍可能继续执行旧代码，因此受保护的写入端还必须检查单调[栅栏令牌](../../Zongsoft.Core/src/Services/Distributing/IDistributedLock.cs)。只在调用方获取锁而不在资源端校验令牌，不能阻止过期持有者写入。

🚨 连接名称只选择服务实例，不会自动成为键前缀。公共接口调用方应使用约定的业务键前缀；如果需要 `EtcdService.Namespace`，应由组合层在首次操作前统一设置。不要在每次业务调用时强转实现类并修改共享配置。

## 独立工具中的直接使用

以下是自行拥有客户端生命周期的低层工具用法，不是模块间协作的默认方式。基础 KV 方法是 Etcd 专有能力；公共序号与锁消费者应采用上一节的接口路径。

以下摘自真实的[序列样例](samples/sequence/Program.cs)，其中 `orders` 和 `score` 是样例输入，不代表已实现的订单处理服务：

```csharp
using Zongsoft.Externals.Etcd;

var connectionString = args.Length > 0 ? args[0] : "server=127.0.0.1;port=2379";
using var sequence = new EtcdService("sample", connectionString) { Namespace = "samples:sequence" };

var number = await sequence.IncreaseAsync("orders", seed: 1000);
var fraction = await sequence.IncreaseAsync("score", 0.25, 1.5);
```

锁获取与续期见独立的[分布式锁样例](samples/distributedlock/master/Program.cs)。


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

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册 Etcd 提供程序、设置驱动与命令组。配置具名设置后，通过提供程序解析序号或锁契约；命名空间及租约选项仍属于应用契约。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Etcd` | [Zongsoft.Externals.Etcd.plugin](src/Zongsoft.Externals.Etcd.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Etcd.deploy](src/Zongsoft.Externals.Etcd.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals etcd]
nuget:Zongsoft.Externals.Etcd
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Etcd.option`、`Zongsoft.Externals.Etcd.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

# Zongsoft.Externals.Redis 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**E**xternals.**R**edis](https://github.com/Zongsoft/framework/tree/main/externals/redis) 将 [Redis](https://redis.io/) 集成到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的基础设施抽象中。它基于 [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) 实现，既可作为插件加载，也可由应用直接引用。

## 主要功能

- 根据 `Redis` 连接设置注册具名 Redis 服务；
- 提供键值、字典、哈希集合、序列和分布式锁操作；
- 基于 Redis 实现框架的消息队列及订阅抽象；
- 在 `/Workspace/Messaging/Storages/Redis` 提供可靠消息存储器工厂；
- 提供 Microsoft 配置提供程序和分布式缓存集成；
- 将 Redis 查询、修改、计数、搜索和锁命令挂载到 Zongsoft 命令树。

加载 `Zongsoft.Externals.Redis.plugin`，并配置 `/Externals/Redis/ConnectionSettings`。消息连接可在 `/Messaging/ConnectionSettings` 下单独配置，两者均使用 `Redis` 驱动器。完整用法可参考[分布式锁示例](samples/distributedlock)、[分布式缓存示例](samples/distributedcache)、[消息示例](samples/messaging)和[测试项目](test)。

使用可靠 Broker 存储时，Redis 连接必须与 Broker 严格同名，并通过插件路径注入工厂。守护插件创建的 ZeroMQ Broker 名为 `QueueServer`：

在进程启动前设置稳定的存储标识：

```powershell
$env:ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER = "broker-storage-01"
```

```xml
<option path="/Externals/Redis">
	<connectionSettings>
		<connectionSetting connectionSetting.name="QueueServer" driver="Redis"
		                   value="server=127.0.0.1:6379;password=;" />
	</connectionSettings>
</option>
<extension path="/Workbench/Messaging/Zero">
	<QueueServer.Storages>{path:/Workspace/Messaging/Storages/Redis}</QueueServer.Storages>
</extension>
```

工厂不会回退默认连接。工厂首次使用时冻结 `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER` 环境变量；该变量为空时回退到 `Environment.MachineName`。存储键以 `Zongsoft.Messaging.Storage:{ConnectionSettings.Name}:{StorageIdentifier}` 为前缀，超长分区使用稳定的 SHA-256 形式。

从旧版 `nodeId` 选项升级时，必须在启动 Broker 前将相同值设置到 `ZONGSOFT_MESSAGING_STORAGE_IDENTIFIER`。值不变时分区文本保持兼容；若未设置，系统可能改用机器名分区，使旧可靠消息留在原前缀下。

消息流默认保留最多 `100000` 条消息，并使用 Redis 的近似裁剪。可通过消息连接设置中的 `MaximumLength` 和 `UseApproximateMaximumLength` 调整；将 `MaximumLength` 设为负数可禁用裁剪。死信搬运使用同槽 Lua 脚本原子完成写入和确认。

缓存变化订阅要求 Redis 启用键空间通知（推荐 `notify-keyspace-events KA`）。通知采用 Redis Pub/Sub 的至多一次语义，断线期间不会重放。

相同连接选项的缓存、消息队列、配置提供程序及 Microsoft 分布式缓存会共享一个 `ConnectionMultiplexer`，各使用方通过独立租约管理生命周期。`RedisService.WithDatabase()` 和 `WithNamespace()` 用于创建不可变作用域；旧的 `Use()`/`Namespace` 只允许在首次使用服务前设置。

通知订阅使用每作用域一个 Redis 后端订阅和每订阅一个有界本地队列，默认容量为 `1024`，默认溢出策略为丢弃最旧项。配置提供程序保存本地快照，并在相关键通知后重新加载。

Redis 分布式锁提供单调递增的栅栏令牌及显式续期。自动续期默认关闭，只能通过 `DistributedLockOptions.RenewalInterval` 显式启用；连接不确定或续期失败时按“锁已丢失”处理，业务写入应校验栅栏令牌。

`RedisServiceInfo.Capabilities` 和 `RedisQueue.Capabilities` 使用所有主节点版本的保守交集：Redis 6.2 启用 `XAUTOCLAIM`，8.2 启用 `XACKDEL`/消费组裁剪能力，8.6 暴露 Stream IDMP 能力；低版本会退回已有路径。诊断源名为 `Zongsoft.Externals.Redis`，同时提供 `ActivitySource` 和 `Meter`，无需额外遥测包。

## 通过公共缓存契约上手

业务模块只需引用 Core 中的 [IDistributedCache](../../Zongsoft.Core/src/Caching/IDistributedCache.cs)，由部署方案选择 Redis 插件。不要把 RedisService 的构造函数作为默认业务入口。

以下宿主选项改写沿用[真实分布式缓存样例](samples/distributedcache/Program.cs)中的 `Redis` 连接名。隔离测试地址与凭据由环境配置提供；这里不另造 Orders 业务模块或缓存选择配置键：

```xml
<options>
	<option path="/Externals/Redis">
		<connectionSettings>
			<connectionSetting connectionSetting.name="Redis" driver="Redis"
			                   value="server=REPLACE_WITH_HOST:REPLACE_WITH_PORT;password=REPLACE_WITH_PASSWORD;database=15" />
		</connectionSettings>
	</option>
</options>
```

下面把样例的缓存读写操作改为通过公共契约在宿主中使用；原样例是自行拥有 RedisService 的独立进程。宿主初始化后，可在服务/命令中使用此片段：

```csharp
using Zongsoft.Caching;
using Zongsoft.Services;

var application = ApplicationContext.Current;
var qualifiedName = "Redis@Redis";
var cache = application.Services.Locate<IDistributedCache>(qualifiedName)
	?? throw new InvalidOperationException("The configured cache is unavailable.");
var key = "Zongsoft.Externals.Redis.Samples:" + Guid.NewGuid().ToString("N");

try
{
	await cache.SetValueAsync(key, "hello", TimeSpan.FromMinutes(1));
	Console.WriteLine(await cache.GetValueAsync<string>(key));
}
finally
{
	await cache.RemoveAsync(key);
}
```

预期输出 `hello`，最后只删除本次生成的键。示例不会清空数据库。模块代码可改用自己的 `Module.Current.Services`；不要逐次释放共享缓存。

### 提供者名与连接名

`Redis@Redis` 的 `Redis` 是注册的提供者别名，`Redis` 是传给其 `GetService(name)` 的连接名；不是名为 Redis 的模块容器。也可显式取得 `Zongsoft.Services.IServiceProvider<IDistributedCache>` 再调用 `GetService("Redis")`，但多个提供者共存时应明确选择提供者，避免注册顺序决定结果。

[RedisServiceProvider](src/RedisServiceProvider.cs) 按名称复用服务。普通缓存/序号/锁服务找不到具名连接时会尝试默认连接；**这不适用于要求严格同名的可靠消息存储工厂**。连接拼写错误可能错误回退，启动检查应核对选定配置，不输出完整连接串。

### 部署版本与首次连接排查

🚨 当前 [部署清单](src/Zongsoft.Externals.Redis.deploy) 先部署 StackExchange.Redis，再部署 Microsoft 缓存适配器。后者的传递依赖可能将同目录 DLL 覆盖成旧版本。本地 .NET 10 验证中，2.7.27 覆盖 3.1.31 后，首次使用出现 `ConfigurationOptions.get_SentinelUser` 缺失错误。插件出现在 `plugin.list` 并不保证首次连接成功。

使用当前源码版本时，可在完成插件部署后、**宿主停止状态下**，用独立补充清单将项目要求的版本重新部署到同一插件目录：

```ini
[plugins zongsoft externals redis]
nuget:StackExchange.Redis@3.1.31
```

```shell
dotnet deploy redis-runtime.deploy --destination:./out --framework:net10.0 --overwrite:alway
```

将以上 INI 保存为 `redis-runtime.deploy`。此命令会覆盖目标文件，目标须是已确认的测试部署目录。版本以当前 [项目文件](src/Zongsoft.Externals.Redis.csproj) 为准，不能把旧修复片段无限沿用；核对最终 DLL 版本并重启后再验证接口读写。

连接失败时依次检查：插件依赖版本 → 具名配置与驱动 → Windows/容器访问地址 → 端口与凭据 → 数据库权限。Windows 宿主访问映射端口用 `127.0.0.1`；容器内的 `localhost` 指向自身，不是 Windows。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册 Redis 服务提供程序、设置驱动、命令及消息存储工厂。配置具名连接后通过提供程序解析缓存/序号/锁契约，不应每次操作重建共享连接。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Externals.Redis` | [Zongsoft.Externals.Redis.plugin](src/Zongsoft.Externals.Redis.plugin) |
| 文件复制及依赖 | [Zongsoft.Externals.Redis.deploy](src/Zongsoft.Externals.Redis.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft externals redis]
nuget:Zongsoft.Externals.Redis
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Externals.Redis.plugin`、`Zongsoft.Externals.Redis.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

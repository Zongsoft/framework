# Redis 分布式锁范例

该范例用于验证 `RedisService.DistributedLock.cs` 中的 Redis 分布式锁实现。

范例包含两个可执行项目：

- `master`：负责重置 Redis 验证状态、自动启动多个 slaver 进程，并汇总计数器。
- `slaver`：负责竞争分布式锁，记录是否有多个工作进程同时进入临界区，并演示基于栅栏令牌的存储写入保护。

两个项目都使用 Zongsoft 的命令执行器和终端输出 API，写法与仓库中其他 samples 保持一致。命令参数由 `Zongsoft.Components.CommandLine` 解析；带值选项请使用 `--name:value` 或 `--name=value` 格式。

## 参考资料

- Redis 官方分布式锁文档：https://redis.io/docs/latest/develop/clients/patterns/distributed-locks/
  获取锁使用 `SET key value NX PX ttl`，释放锁使用 Lua 脚本比较 token 后再删除。
- RedLock.net：https://github.com/samcook/redlock.net
- redlock-rb：https://github.com/leandromoreira/redlock-rb
- node-redlock：https://github.com/mike-marcacci/node-redlock

## 验证场景

- `mutex`：临界区执行时间短于锁有效期。预期不会出现重叠进入临界区。
- `expiry`：临界区执行时间故意超过锁有效期。预期至少出现一次重叠，以证明该范例能暴露 TTL 窗口失效风险。
- `renew`：临界区执行时间同样超过锁有效期，但启用了自动续期（`--renewal-interval`）。预期不会出现重叠，以证明续期可以维持锁的所有权。

## 栅栏令牌与过期写入

每次成功获取锁都会返回单调递增的 `IDistributedLock.FencingToken`。slaver 会模拟一次由栅栏令牌保护的共享存储写入：写入前将当前令牌与存储中已记录的最大令牌比较，只有当前令牌不小于存储中的最大令牌时写入才被接受，否则作为过期写入被拒绝。

- 在 `mutex` 和 `renew` 场景中锁从未丢失，因此所有写入都会被接受，`Stale` 计数器保持为零。
- 在 `expiry` 场景中，第一个持有者仍在工作时锁已过期；第二个持有者带着更大的栅栏令牌进入后，第一个持有者的迟到写入会被拒绝。此时 `Violations` 和 `Stale` 都预期大于零，证明栅栏令牌可以捕获锁本身无法阻止的过期写入。

## 构建

先构建两个项目：

```pwsh
dotnet build externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug
dotnet build externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug
```

默认 Redis 地址为 `127.0.0.1:6379`，数据库为 `15`，密码优先读取 `REDIS_PASSWORD` 环境变量，未设置时使用 `xxxxxx`。

```pwsh
$env:REDIS_PASSWORD = "xxxxxx"
```

## 自动验证

master 可以自动启动多个 slaver 进程。

正常互斥场景：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- run --workers:8 --iterations:80
```

锁过期风险场景：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- run --scenario:expiry --workers:8 --iterations:12
```

自动续期场景（临界区超过有效期，但自动续期维持锁）：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- run --scenario:renew --workers:8 --iterations:12
```

## 人工验证

所有命令使用同一个 `run-id`。命名空间会生成为 `DistributedLock:<run-id>:<scenario>`。

1. 在 master 终端重置状态：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-001 --scenario:mutex
```

2. 打开多个 slaver 终端，每个终端使用不同的 `worker-id`：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:1 --iterations:40
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:2 --iterations:40
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-001 --scenario:mutex --worker-id:3 --iterations:40
```

3. slaver 全部结束后，在 master 终端汇总。`expected` 为 `worker-count * iterations`：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-001 --scenario:mutex --expected:120
```

验证锁过期风险时，reset、slaver、report 命令都改用 `--scenario:expiry`。report 会预期出现 violations 和过期写入：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-expiry --scenario:expiry
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:1 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:2 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-expiry --scenario:expiry --expected:16
```

验证自动续期时，改用 `--scenario:renew`。report 预期不会出现 violations 和过期写入：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-renew --scenario:renew
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-renew --scenario:renew --worker-id:1 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-renew --scenario:renew --worker-id:2 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-renew --scenario:renew --expected:16
```

任意命令都可以通过 `--connection` 指定完整 Redis 连接字符串：

```pwsh
--connection:"server=127.0.0.1;port=6379;password=xxxxxx;database=15;"
```

## 选项说明

| 选项 | 适用命令 | 说明 |
| --- | --- | --- |
| `--scenario:mutex\|expiry\|renew` | 全部 | 选择场景默认值；默认为 `mutex`。 |
| `--expiry:<timespan>` | run | 锁的有效时长；覆盖场景默认值。 |
| `--hold:<timespan>` | run | 每次迭代的临界区时长；覆盖场景默认值。 |
| `--renewal-interval:<timespan>` | run | 以指定间隔启用自动续期；必须小于锁的有效时长。 |
| `--verbose` | slaver run | 打印每次进入临界区和栅栏写入的详细信息。 |
| `--expected:<count>` | report | 预期的 `entered`/`completed` 数量（`workers * iterations`）。 |
| `--expect-violations` | report | 将 violations 和过期写入视为预期结果；`--scenario:expiry` 默认启用。 |

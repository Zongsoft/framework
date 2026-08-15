# Redis 分布式锁范例

该范例用于验证 `RedisService.DistributedLock.cs` 中的 Redis 分布式锁实现。

范例包含两个可执行项目：

- `master`：负责重置 Redis 验证状态、自动启动多个 slaver 进程，并汇总计数器。
- `slaver`：负责竞争分布式锁，并记录是否有多个工作进程同时进入临界区。

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

验证锁过期风险时，reset、slaver、report 命令都改用 `--scenario:expiry`。report 会预期出现 violations：

```pwsh
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- reset --run-id:manual-expiry --scenario:expiry
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:1 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\slaver\Zongsoft.Externals.Redis.DistributedLock.Slaver.csproj -c Debug -- run --run-id:manual-expiry --scenario:expiry --worker-id:2 --iterations:8
dotnet run --project externals\redis\samples\DistributedLock\master\Zongsoft.Externals.Redis.DistributedLock.Master.csproj -c Debug -- report --run-id:manual-expiry --scenario:expiry --expected:16
```

任意命令都可以通过 `--connection` 指定完整 Redis 连接字符串：

```pwsh
--connection:"server=127.0.0.1;port=6379;password=xxxxxx;database=15;"
```

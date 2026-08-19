# Redis 分布式缓存范例

该范例演示 `RedisService.DistributedCache.cs` 中的 Redis 分布式缓存实现。它是一个交互式终端客户端，将缓存操作暴露为命令，写法与[消息范例](../messaging)保持一致。

范例覆盖以下内容：

- 键值操作：带可选过期时间和设置条件的 set、get、exists、remove。
- 过期管理：查询剩余生存时长、延长或设为永不过期。
- 缓存查看：统计条目总数、按模式查找键。
- 缓存通知：订阅键空间通知，并打印每条收到的 `DistributedCacheNotification`。

## 环境要求

- 可连接的 Redis 服务。默认地址为 `127.0.0.1:6379`，密码为 `xxxxxx`；如需修改请编辑 `Program.cs` 中的连接字符串。
- 订阅命令要求 Redis 启用包含 `K` 和 `A` 事件的键空间通知（推荐 `notify-keyspace-events KA`）。set/get/remove 等命令无需该配置。

## 构建与运行

```pwsh
dotnet run --project externals\redis\samples\distributedcache\Zongsoft.Externals.Redis.DistributedCache.Samples.csproj -c Debug
```

所有缓存键都会加上 `DistributedCache` 命名空间前缀，因此一个客户端写入的条目对使用相同命名空间的其他客户端可见——可打开多个终端来观察跨进程的通知。

## 命令说明

### 写入与读取

`set` 写入缓存值，`--expiry` 选项指定生存时长，`--requisite` 选项约束写入条件：

```text
set --key:greeting hello
set --key:token --expiry:30s "a temporary value"
set --key:config --requisite:notexists "created only if absent"
set --key:config --requisite:exists "updated only if present"
```

`get` 输出缓存值及其剩余生存时长：

```text
get greeting
get token
```

`exists` 与 `remove`（别名 `del`）：

```text
exists greeting
remove greeting
```

### 过期管理

不带 `--expiry` 的 `expiry` 命令输出剩余生存时长；带 `--expiry` 时设置生存时长（`0` 表示永不过期）：

```text
expiry token
expiry greeting --expiry:1h
expiry greeting --expiry:0
```

### 缓存查看

```text
count
find gre*
info
```

`count` 统计命名空间内的条目数，`find` 列出匹配模式的键（默认为 `*`），`info` 显示服务名、命名空间、数据库、条目数和订阅状态。`purge` 删除命名空间内的所有条目：

```text
purge
```

### 订阅通知

`subscribe`（别名 `sub`）注册通知处理程序。可选的 `--prefix` 按逻辑键前缀过滤，`--kind` 选择 `updated`、`removed`、`expired`、`evicted` 或 `all`（默认）：

```text
subscribe
subscribe --prefix:orders:
subscribe --kind:updated --prefix:telemetry:
```

订阅成功后，本进程或共享相同命名空间的其他进程产生的匹配变化都会以 `[Received] Kind:... Key:...` 格式打印。`unsubscribe`（别名 `unsub`）取消订阅，`reset` 清空已接收通知计数。

## 建议场景

打开两个终端。第一个终端订阅，然后使用第二个终端修改条目并观察通知：

```text
subscribe --kind:updated
```

```text
set --key:orders:1001 "in progress"
set --key:orders:1002 --expiry:10s "completed"
remove orders:1001
```

第一个终端应观察到两条 `set` 命令产生的 `Updated` 通知和一条 `remove` 命令产生的 `Removed` 通知。使用 `info` 确认订阅状态，最后用 `unsubscribe` 停止接收通知。

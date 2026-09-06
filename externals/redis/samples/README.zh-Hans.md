# Zongsoft.Externals.Redis 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [messaging](messaging) | 演示基于 Redis 的消息发布、标签订阅、消息确认和队列信息查看。 |
| [distributedcache](distributedcache) | 演示 Redis 分布式缓存的键值操作、过期管理与键空间变化通知。 |
| [distributedlock](distributedlock) | 通过多个进程竞争 Redis 分布式锁，验证互斥、锁过期行为、自动续期与栅栏令牌。 |

所有项目均面向 .NET 10，并要求 Redis 服务可连接。运行前请检查各范例中的连接字符串；消息范例默认连接 `127.0.0.1:6379`，密码为 `xxxxxx`。

## 消息范例

在仓库根目录运行交互式消息客户端：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -c Debug -f net10.0 -p:GeneratePackageOnBuild=false
dotnet run --project externals/redis/samples/messaging/Zongsoft.Externals.Redis.Messaging.Samples.csproj
```

Debug 项目引用本地构建的 Core DLL。[messaging/Program.cs](messaging/Program.cs) 自行持有具体的诊断队列；应用消费者应采用[插件与公共提供者方式](../README.zh-Hans.md)。

### 订阅与取消订阅

传入一个或多个主题即可订阅，`sub` 是 `subscribe` 的别名：

```text
subscribe orders invoices
subscribe --tags:urgent notifications
sub --tags:region-a telemetry
```

可选的 `--tags` 值会传给 Redis 订阅者，用于按标签过滤。使用 `unsubscribe` 或 `unsub` 取消订阅：

```text
unsubscribe invoices notifications
```

### 发布消息

必须通过 `--topic` 选项指定目标主题，每个位置参数都会作为一条独立的 UTF-8 消息发布：

```text
produce --topic:orders "order #1001"
produce --topic:orders first second third
produce --topic:notifications --tags:urgent "service unavailable"
```

使用 `--round:<次数>` 重复发布每个参数，`send` 是 `produce` 的别名：

```text
produce --topic:orders --round:3 hello
send --topic:telemetry --tags:region-a --round:2 value-1 value-2
```

范例会在消息正文前添加轮次编号，输出返回的消息标识和耗时，并在处理器显示消息后进行确认。

### 查看与重置

```text
info
reset
close
```

`info` 显示队列和所有当前订阅，包括订阅标签；`reset` 清空已接收消息计数；`close` 释放队列，执行后需重启进程才能继续使用消息命令。

### 建议场景

使用匹配标签逐条执行。发布前等待订阅完成，取消订阅前等待接收输出：

```text
subscribe --tags:urgent alerts
produce --topic:alerts --tags:urgent --round:3 "High temperature"
info
unsubscribe alerts
```

应观察到三条接收消息，并在取消前看到带有 `urgent` 标签的 `alerts` 订阅。

💡 各范例进程默认使用同一个消费者组，多个客户端会竞争流条目，而不是每个客户端都收到三条消息。这个观察场景使用一个消费客户端和唯一测试主题；显示的计数不是恰好一次投递保证。

### 安全与清理

🚨 仅使用专用本地 Redis 数据库和虚拟消息。客户端退出后流、组和待确认条目可能仍然存在；确认消息不代表删除流。取消本次订阅后执行 `close`，再执行 `exit` 并确认。如需清理持久资源，先检查实际流键，再通过 Redis 管理工具仅删除本次测试创建的资源；不要清空共享数据库。

## 分布式缓存范例

分布式缓存范例是键值操作、过期管理与变化通知的交互式客户端。命令参考及双终端通知场景参见其[完整说明](distributedcache/README.zh-Hans.md)。

## 分布式锁范例

分布式锁范例包含相互协作的主进程和从进程。构建命令、自动与手动测试场景以及连接覆盖方法参见其[完整说明](distributedlock/README.zh-Hans.md)。

# Zongsoft.Externals.Redis 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 项目

| 项目 | 用途 |
| --- | --- |
| [messaging](messaging) | 演示基于 Redis 的消息发布、标签订阅、消息确认和队列信息查看。 |
| [distributedlock](distributedlock) | 通过多个进程竞争 Redis 分布式锁，验证互斥与锁过期行为。 |

所有项目均面向 .NET 10，并要求 Redis 服务可连接。运行前请检查各范例中的连接字符串；消息范例默认连接 `127.0.0.1:6379`，密码为 `xxxxxx`。

## 消息范例

在仓库根目录运行交互式消息客户端：

```shell
dotnet run --project externals/redis/samples/messaging/Zongsoft.Externals.Redis.Messaging.Samples.csproj
```

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

使用匹配标签依次执行：

```text
subscribe --tags:urgent alerts
produce --topic:alerts --tags:urgent --round:3 "High temperature"
info
unsubscribe alerts
```

应观察到三条接收消息，并在取消前看到带有 `urgent` 标签的 `alerts` 订阅。

## 分布式锁范例

分布式锁范例包含相互协作的主进程和从进程。构建命令、自动与手动测试场景以及连接覆盖方法参见其[完整说明](distributedlock/README.zh-Hans.md)。

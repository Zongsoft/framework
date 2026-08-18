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
- 提供 Microsoft 配置提供程序和分布式缓存集成；
- 将 Redis 查询、修改、计数、搜索和锁命令挂载到 Zongsoft 命令树。

加载 `Zongsoft.Externals.Redis.plugin`，并配置 `/Externals/Redis/ConnectionSettings`。消息连接可在 `/Messaging/ConnectionSettings` 下单独配置，两者均使用 `Redis` 驱动器。完整用法可参考[分布式锁示例](samples/distributedlock)、[消息示例](samples/messaging)和[测试项目](test)。

消息流默认保留最多 `100000` 条消息，并使用 Redis 的近似裁剪。可通过消息连接设置中的 `MaximumLength` 和 `UseApproximateMaximumLength` 调整；将 `MaximumLength` 设为负数可禁用裁剪。死信搬运使用同槽 Lua 脚本原子完成写入和确认。

缓存变化订阅要求 Redis 启用键空间通知（推荐 `notify-keyspace-events KA`）。通知采用 Redis Pub/Sub 的至多一次语义，断线期间不会重放。

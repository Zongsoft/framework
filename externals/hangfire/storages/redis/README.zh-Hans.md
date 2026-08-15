# Zongsoft.Externals.Hangfire.Storages.Redis 扩展插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Storages.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Storages.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

`Zongsoft.Externals.Hangfire.Storages.Redis` 为 [Zongsoft Hangfire 集成](../..)提供基于 Redis 的持久化存储。它将 `Hangfire.Redis.StackExchange` 适配到 Zongsoft 连接设置，并把生成的 `RedisStorage` 注册为 Hangfire 的 `JobStorage`。

## 配置

请同时加载 `Zongsoft.Externals.Hangfire.Storages.Redis.plugin`、Hangfire 核心插件和 Redis 插件。适配器读取 `/Externals/Redis/ConnectionSettings`，优先使用名称为 `Hangfire` 且驱动器为 `Redis` 的连接设置；若不存在，则回退到默认设置或首个 Redis 设置。

请在启动 Hangfire 服务器前配置并验证 Redis 连接。所有 Hangfire 作业、状态、队列和服务器元数据都会通过该连接持久化。

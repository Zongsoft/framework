# Zongsoft.Externals.Hangfire.Storages.Redis Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Hangfire.Storages.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Hangfire.Storages.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

`Zongsoft.Externals.Hangfire.Storages.Redis` provides Redis-backed persistent storage for the [Zongsoft Hangfire integration](../..). It adapts `Hangfire.Redis.StackExchange` to Zongsoft connection settings and registers the resulting `RedisStorage` as Hangfire's `JobStorage`.

## Configuration

Load `Zongsoft.Externals.Hangfire.Storages.Redis.plugin` together with the core Hangfire and Redis plugins. The adapter reads `/Externals/Redis/ConnectionSettings`, preferring a setting named `Hangfire` whose driver is `Redis`; otherwise it falls back to the default or first Redis setting.

Configure and verify the Redis connection before starting the Hangfire server. All Hangfire jobs, states, queues, and server metadata are persisted through that connection.

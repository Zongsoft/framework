# Zongsoft.Externals.Redis Extension Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Externals.Redis)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Externals.Redis)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**E**xternals.**R**edis](https://github.com/Zongsoft/framework/tree/main/externals/redis) integrates [Redis](https://redis.io/) with the infrastructure abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. It is built on [StackExchange.Redis](https://github.com/StackExchange/StackExchange.Redis) and can be used as a plugin or referenced directly by an application.

## Features

- Registers named Redis services from `Redis` connection settings.
- Provides key/value, dictionary, hash-set, sequence, and distributed-lock operations.
- Implements the framework's message queue and subscription abstractions with Redis.
- Supplies a Microsoft configuration provider and distributed-cache integration.
- Adds Redis inspection, mutation, counter, search, and lock commands to the Zongsoft command tree.

Load `Zongsoft.Externals.Redis.plugin` and configure `/Externals/Redis/ConnectionSettings`. Messaging connections can be configured separately under `/Messaging/ConnectionSettings`; both use the `Redis` driver. See the [distributed-lock sample](samples/distributedlock), the [messaging sample](samples/messaging), and the [tests](test) for working examples.

# Zongsoft.Messaging.RabbitMQ Message Queue Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.RabbitMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.RabbitMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**M**essaging.**RabbitMQ**](https://github.com/Zongsoft/framework/tree/main/messaging/rabbit) adapts [RabbitMQ](https://www.rabbitmq.com/) to the messaging abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. It uses `RabbitMQ.Client` to provide queue creation, message publishing, and subscription support behind a consistent application-facing API.

## Features

- Implements the Zongsoft message queue provider, queue, and subscriber abstractions.
- Supports RabbitMQ connection settings through the `RabbitMQ` connection-setting driver.
- Packages the plugin manifest and default option file required by a Zongsoft host.

Load `Zongsoft.Messaging.RabbitMQ.plugin`, configure a connection setting whose driver is `RabbitMQ`, and obtain the queue through the framework's messaging services. See the [sample project](samples) for a minimal host and the [tests](test) for publishing, subscription, and concurrency examples.

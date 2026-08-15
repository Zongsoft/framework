# Zongsoft.Messaging.Kafka Message Queue Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Kafka)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.Kafka)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

[**Z**ongsoft.**M**essaging.**K**afka](https://github.com/Zongsoft/framework/tree/main/messaging/kafka) adapts [Apache Kafka](https://kafka.apache.org/) to the messaging abstractions of the [_**Z**ongsoft_](https://github.com/Zongsoft/framework) framework. It uses `Confluent.Kafka` to provide queue creation, message publishing, and subscription support without coupling application code to the Kafka client API.

## Features

- Implements the Zongsoft message queue provider, queue, and subscriber abstractions.
- Supports Kafka connection settings through the `Kafka` connection-setting driver.
- Packages the plugin manifest and default option file required by a Zongsoft host.

Load `Zongsoft.Messaging.Kafka.plugin`, configure a connection setting whose driver is `Kafka`, and obtain the queue through the framework's messaging services. See the [sample project](samples) for a minimal host and the [tests](test) for publishing, subscription, and concurrency examples.

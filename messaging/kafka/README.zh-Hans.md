# Zongsoft.Messaging.Kafka 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.Kafka)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.Kafka)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**M**essaging.**K**afka](https://github.com/Zongsoft/framework/tree/main/messaging/kafka) 将 [Apache Kafka](https://kafka.apache.org/) 适配到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的消息队列抽象。它基于 `Confluent.Kafka` 提供队列创建、消息发布和订阅能力，使应用代码无需直接依赖 Kafka 客户端 API。

## 主要功能

- 实现 Zongsoft 的消息队列提供程序、队列及订阅者抽象；
- 通过 `Kafka` 连接设置驱动器读取 Kafka 连接参数；
- 随程序包提供 Zongsoft 宿主所需的插件清单和默认选项文件。

加载 `Zongsoft.Messaging.Kafka.plugin`，配置驱动器为 `Kafka` 的连接设置，即可通过框架的消息服务获取队列。最小宿主可参考[示例项目](samples)，发布、订阅和并发用法可参考[测试项目](test)。

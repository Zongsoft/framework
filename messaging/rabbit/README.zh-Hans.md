# Zongsoft.Messaging.RabbitMQ 消息队列插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.RabbitMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.RabbitMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**M**essaging.**RabbitMQ**](https://github.com/Zongsoft/framework/tree/main/messaging/rabbit) 将 [RabbitMQ](https://www.rabbitmq.com/) 适配到 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 框架的消息队列抽象。它基于 `RabbitMQ.Client` 提供队列创建、消息发布和订阅能力，并向应用层提供一致的访问方式。

## 主要功能

- 实现 Zongsoft 的消息队列提供程序、队列及订阅者抽象；
- 通过 `RabbitMQ` 连接设置驱动器读取 RabbitMQ 连接参数；
- 随程序包提供 Zongsoft 宿主所需的插件清单和默认选项文件。

加载 `Zongsoft.Messaging.RabbitMQ.plugin`，配置驱动器为 `RabbitMQ` 的连接设置，即可通过框架的消息服务获取队列。最小宿主可参考[示例项目](samples)，发布、订阅和并发用法可参考[测试项目](test)。


## 安装与配置

```shell
dotnet add package Zongsoft.Messaging.RabbitMQ
```

```xml
<option path="/Messaging">
	<connectionSettings>
		<connectionSetting connectionSetting.name="rabbit"
		                   driver="RabbitMQ"
		                   value="server=127.0.0.1;client=application;group=workers" />
	</connectionSettings>
</option>
```

适配器设置包括 `server`、`port`、`username`、`password`、`container`（虚拟主机）、`queue`、`group`、`client`、`heartbeat`、`timeout`、`concurrency`、`certificate` 与 `reconnectable`。秘密应由部署配置提供；示例有意省略凭据。

## 投递模型

适配器按框架 Topic 与 Tag 声明/使用 Broker 的 Exchange、Queue 和 Binding。消费采用手动确认；`Message.AcknowledgeAsync` 发送 `basic.ack`。压缩方式使用 AMQP Content Encoding。

处理器只应在副作用安全完成后确认消息。未确认工作可在 Broker 恢复或重启后重新投递；即使顺利路径看似只执行一次，应用也需要幂等处理器或去重键。

> 💡 队列声明 `MessageQueueFeature.Compression`。能力存在表示适配器实现了框架压缩契约，并不表示每条消息都会压缩。

## 生命周期与可靠性

请通过框架提供程序创建队列，以保持连接命名和释放一致。需要消费期间必须保留订阅对象，并在优雅停机时释放/关闭。处理器应传递取消信号，但也要处理“取消与 Broker 确认同时发生”的不确定状态。

🚨 Topic、权限、保留、死信、最大载荷、TLS 和可用性属于 Broker 运维配置。适配器不会把单节点 Broker 变成高可用持久部署，生产环境绝不能使用范例凭据。

## 验证与资料

在可丢弃 Broker 上运行[交互范例](samples/README.zh-Hans.md)，验证发布、接收、确认、退订、重连、重复处理和压缩。集成测试要求显式启用真实 Broker。

- [消息实现协作指南](../SKILL.md)
- [RabbitMQ 文档](https://www.rabbitmq.com/docs)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册连接设置驱动，服务发现注册队列提供程序。解析 IMessageQueueProvider 前配置具名连接。Broker 位于外部，加载插件不会创建主题、用户或 Broker 服务。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Messaging.RabbitMQ` | [Zongsoft.Messaging.RabbitMQ.plugin](src/Zongsoft.Messaging.RabbitMQ.plugin) |
| 文件复制及依赖 | [Zongsoft.Messaging.RabbitMQ.deploy](src/Zongsoft.Messaging.RabbitMQ.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft messaging rabbitmq]
nuget:Zongsoft.Messaging.RabbitMQ
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Messaging.RabbitMQ.plugin`、`Zongsoft.Messaging.RabbitMQ.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

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


## 安装与配置

```shell
dotnet add package Zongsoft.Messaging.Kafka
```

```xml
<option path="/Messaging">
	<connectionSettings>
		<connectionSetting connectionSetting.name="kafka"
		                   driver="Kafka"
		                   value="server=127.0.0.1;client=application;group=workers" />
	</connectionSettings>
</option>
```

适配器设置包括 `server`、`username`、`password`、`client`、`group`、`securityProtocol`、`compressionType`、`compressionLevel`、`isolationLevel`、`heartbeat`、`timeout` 及事务设置。秘密应由部署配置提供；示例有意省略凭据。

## 投递模型

Topic 对应 Kafka Topic，`group` 控制消费组分摊。收到的消息通过提交已消费 Offset 确认；压缩方式写入适配器 Header，并在处理器执行前解压。

处理器只应在副作用安全完成后确认消息，并为重复处理准备幂等或去重机制。

🚨 当前 [GetConsumerOptions](src/Configuration/KafkaConnectionSettings.cs) 没有关闭 Kafka 客户端自动提交/自动记录 Offset；显式确认调用 Commit，并不意味着未确认的消息一定会重新投递。不要据此宣称端到端至少一次或恰好一次。需要严格的业务确认语义时，应先核对并验证实际 ConsumerConfig 与重启恢复行为；不要编造尚未暴露的配置开关。

> 💡 队列声明 `MessageQueueFeature.Compression`。能力存在表示适配器实现了框架压缩契约，并不表示每条消息都会压缩。

## 生命周期与可靠性

请通过框架提供程序创建队列，以保持连接命名和释放一致。需要消费期间必须保留订阅对象，并在优雅停机时释放/关闭。处理器应传递取消信号，但也要处理“取消与 Broker 确认同时发生”的不确定状态。

🚨 Topic、权限、保留、死信、最大载荷、TLS 和可用性属于 Broker 运维配置。适配器不会把单节点 Broker 变成高可用持久部署，生产环境绝不能使用范例凭据。

## 验证与资料

在可丢弃 Broker 上运行[交互范例](samples/README.zh-Hans.md)，验证发布、接收、确认、退订、重连、重复处理和压缩。集成测试要求显式启用真实 Broker。

- [消息实现协作指南](../SKILL.md)
- [Kafka 文档](https://kafka.apache.org/documentation/)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../Zongsoft.Plugins/README.zh-Hans.md)。

清单注册连接设置驱动，服务发现注册队列提供程序。解析 IMessageQueueProvider 前配置具名连接。Broker 位于外部，加载插件不会创建主题、用户或 Broker 服务。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Messaging.Kafka` | [Zongsoft.Messaging.Kafka.plugin](src/Zongsoft.Messaging.Kafka.plugin) |
| 文件复制及依赖 | [Zongsoft.Messaging.Kafka.deploy](src/Zongsoft.Messaging.Kafka.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft messaging kafka]
nuget:Zongsoft.Messaging.Kafka
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Messaging.Kafka.plugin`、`Zongsoft.Messaging.Kafka.option`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

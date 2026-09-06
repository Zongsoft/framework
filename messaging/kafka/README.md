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


## Installation and Configuration

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

Supported adapter settings include `server`, `username`, `password`, `client`, `group`, `securityProtocol`, `compressionType`, `compressionLevel`, `isolationLevel`, `heartbeat`, `timeout`, and transaction settings. Supply secrets through deployment configuration; the example intentionally omits credentials.

## Delivery Model

A topic is a Kafka topic; `group` controls consumer-group sharing. Received messages acknowledge by committing the consumed offset. Compression is conveyed through the adapter header and decompressed before the handler runs.

A handler should acknowledge only after its side effects are safely complete, and use idempotent processing or deduplication for repeated work.

🚨 The current [GetConsumerOptions](src/Configuration/KafkaConnectionSettings.cs) does not disable the Kafka client's automatic commit/offset storage. Explicit acknowledgement calls Commit, but withholding it does not guarantee redelivery. Do not infer end-to-end at-least-once or exactly-once guarantees. Strict business acknowledgement requires checking the actual ConsumerConfig and testing restart recovery; do not rely on configuration switches the adapter does not expose.

> 💡 The queue advertises `MessageQueueFeature.Compression`. Feature presence means the adapter implements the framework compression contract; it does not mean every message is compressed.

## Lifecycle and Reliability

Create queues through the framework provider so connection naming and disposal remain consistent. Keep a subscription object alive for as long as consumption is wanted, and dispose/close it during graceful shutdown. Propagate cancellation from handlers, but design for ambiguity when cancellation races with broker acknowledgement.

🚨 Broker topics, permissions, retention, dead-lettering, maximum payload size, TLS, and availability are operational configuration. The adapter does not turn a single broker into a durable highly available deployment. Never use the sample credentials in production.

## Verification and Resources

Run the [interactive sample](samples/README.md) against a disposable broker and verify publish, receive, acknowledge, unsubscribe, reconnect, duplicate handling, and compression. Integration tests require an explicitly enabled real broker.

- [Messaging implementation guidance](../SKILL.md)
- [Kafka documentation](https://kafka.apache.org/documentation/)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest registers the connection-settings driver, and service discovery registers the queue provider. Configure a named connection before resolving IMessageQueueProvider. The broker is external; loading the plugin does not create topics, users or a broker service.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Messaging.Kafka` | [Zongsoft.Messaging.Kafka.plugin](src/Zongsoft.Messaging.Kafka.plugin) |
| File copying and dependencies | [Zongsoft.Messaging.Kafka.deploy](src/Zongsoft.Messaging.Kafka.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft messaging kafka]
nuget:Zongsoft.Messaging.Kafka
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Messaging.Kafka.plugin`, `Zongsoft.Messaging.Kafka.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

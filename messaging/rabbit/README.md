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


## Installation and Configuration

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

Supported adapter settings include `server`, `port`, `username`, `password`, `container` (virtual host), `queue`, `group`, `client`, `heartbeat`, `timeout`, `concurrency`, `certificate`, and `reconnectable`. Supply secrets through deployment configuration; the example intentionally omits credentials.

## Delivery Model

The adapter declares/uses broker exchanges, queues, and bindings for framework topics and tags. Consumption uses manual acknowledgement; `Message.AcknowledgeAsync` sends `basic.ack`. Compression uses AMQP content encoding.

A handler should acknowledge only after its side effects are safely complete. Without acknowledgement, broker recovery or restart can redeliver work. Applications therefore need idempotent handlers or a deduplication key even when the happy path appears exactly once.

> 💡 The queue advertises `MessageQueueFeature.Compression`. Feature presence means the adapter implements the framework compression contract; it does not mean every message is compressed.

## Lifecycle and Reliability

Create queues through the framework provider so connection naming and disposal remain consistent. Keep a subscription object alive for as long as consumption is wanted, and dispose/close it during graceful shutdown. Propagate cancellation from handlers, but design for ambiguity when cancellation races with broker acknowledgement.

🚨 Broker topics, permissions, retention, dead-lettering, maximum payload size, TLS, and availability are operational configuration. The adapter does not turn a single broker into a durable highly available deployment. Never use the sample credentials in production.

## Verification and Resources

Run the [interactive sample](samples/README.md) against a disposable broker and verify publish, receive, acknowledge, unsubscribe, reconnect, duplicate handling, and compression. Integration tests require an explicitly enabled real broker.

- [Messaging implementation guidance](../SKILL.md)
- [RabbitMQ documentation](https://www.rabbitmq.com/docs)

## Plugin-Based Integration

Compose this feature through the host; a package reference supplies compile-time APIs, while plugin loading also requires deployed manifests and runtime assets. See [the complete plugin workflow](../../Zongsoft.Plugins/README.md).

The manifest registers the connection-settings driver, and service discovery registers the queue provider. Configure a named connection before resolving IMessageQueueProvider. The broker is external; loading the plugin does not create topics, users or a broker service.

| Runtime artifact | Source of truth |
| --- | --- |
| `Zongsoft.Messaging.RabbitMQ` | [Zongsoft.Messaging.RabbitMQ.plugin](src/Zongsoft.Messaging.RabbitMQ.plugin) |
| File copying and dependencies | [Zongsoft.Messaging.RabbitMQ.deploy](src/Zongsoft.Messaging.RabbitMQ.deploy) |

Add this fragment to an existing host `.deploy` (retain Main and the host’s other base manifests; do not replace the whole file):

```ini
[plugins zongsoft messaging rabbitmq]
nuget:Zongsoft.Messaging.RabbitMQ
```

Run `dotnet deploy` against a test deployment as explained in the workflow, with the host's `framework`, `platform`, `architecture` and, where needed, `site`. Pin compatible versions in real deployments; application dependencies such as databases, caches or commercial runtimes are still separate prerequisites.

Additional artifacts listed by the deployment manifest include `Zongsoft.Messaging.RabbitMQ.plugin`, `Zongsoft.Messaging.RabbitMQ.option`. Retain assemblies, dependencies and satellite resource directories as well. Restart the host after deployment, check plugin loading and service/driver registration, then verify the workflow above; copied files alone do not prove that the feature is active.

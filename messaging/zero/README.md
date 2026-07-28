# Zongsoft.Messaging.ZeroMQ Message Queue Plugin Library

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Messaging.ZeroMQ)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Messaging.ZeroMQ)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Samples

The `samples` directory contains two interactive .NET 10 applications:

- [Server sample](samples/server/Program.cs) starts the ZeroMQ message exchange powered by `ZeroQueueServer`.
- [Client sample](samples/client/Program.cs) connects through `ZeroQueue` and can subscribe, unsubscribe, publish, and receive messages.

The server listens for exchange discovery requests on port `7969` and starts the sample data channels on ports `32101` and `32102`. The client connects to `127.0.0.1:7969` and uses the `Demo` message group by default.

> The Server sample binds to all network interfaces without authentication or encryption. Use it only in a trusted development or test environment.

### Prerequisites and build

Install the .NET 10 SDK. From the repository root, build the core library first and then the ZeroMQ solution:

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -f net10.0
dotnet build messaging/zero/Zongsoft.Messaging.ZeroMQ.slnx
```

### 1. Start the Server

Open the first terminal and run:

```shell
dotnet run --project messaging/zero/samples/server/Zongsoft.Messaging.ZeroMQ.Samples.Server.csproj
```

The exchange starts automatically with management port `7969`, incoming data port `32101`, and outgoing data port `32102`.

| Command | Description |
| --- | --- |
| `info` | Show the Server state and management port. |
| `stop` | Stop the exchange and release all three ports. |
| `start --incoming:32101 --outgoing:32102` | Restart the exchange with the sample data ports. |

### 2. Start a subscriber

Open a second terminal and start the Client sample:

```shell
dotnet run --project messaging/zero/samples/client/Zongsoft.Messaging.ZeroMQ.Samples.Client.csproj
```

Subscribe to a test topic:

```text
subscribe samples/demo
```

The client adds the configured `Demo` group to the network topic automatically. All Client sample instances use the same group and can communicate with each other.

### 3. Publish and verify a message

Open a third terminal and start another Client sample with the same `dotnet run` command. Each queue derives a unique instance identifier, so both clients can remain connected. Publish a message:

```text
produce --topic:samples/demo "Hello from the ZeroMQ sample"
```

The alias `send` can be used instead of `produce`. The publishing client prints the topic and elapsed time. The subscriber terminal should display output similar to:

```text
[Received]#1 Topic:Demo:samples/demo
[1]Hello from the ZeroMQ sample
```

### Batch verification

Use the `round` option to publish the same payload repeatedly:

```text
produce --topic:samples/demo --round:1000 "Load test message"
```

The subscriber displays every received message with an incrementing counter, and the publishing client reports the total elapsed time for a quick throughput check.

### Restart verification

Keep both clients running, then enter the following commands in the Server terminal:

```text
stop
start --incoming:32101 --outgoing:32102
```

The NetMQ sockets reconnect to the restored endpoints automatically. Publish another message and verify that the existing subscriber receives it without subscribing again.

### Client commands

| Command | Description |
| --- | --- |
| `info` | Show the queue instance identifier, connection settings, and active subscriptions. |
| `subscribe <topic> [...]` | Subscribe to one or more topics. Alias: `sub`. |
| `unsubscribe <topic> [...]` | Remove one or more subscriptions. Alias: `unsub`. |
| `produce --topic:<topic> [--round:<count>] <message> [...]` | Publish one or more messages. Alias: `send`. |
| `reset` | Reset the displayed receive counter. |
| `close` | Dispose the ZeroMQ queue; restart the Client sample to continue testing. |

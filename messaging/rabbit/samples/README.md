# Zongsoft.Messaging.RabbitMQ Samples

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## Overview

This interactive sample demonstrates the `Zongsoft.Messaging.RabbitMQ` queue, publisher, subscriber, and message acknowledgment APIs.

The project targets .NET 10 and expects RabbitMQ at `127.0.0.1:5672`, using the user `program` and password `xxxxxx`. Update the connection settings in `Program.cs` to match the local broker before running the sample.

## Run

From the repository root:

```shell
dotnet run --project messaging/rabbit/samples/Zongsoft.Messaging.RabbitMQ.Samples.csproj
```

## Subscribe and Unsubscribe

Subscribe to one or more topics by passing each topic as an argument. `sub` is an alias for `subscribe`:

```text
subscribe orders invoices
sub notifications
```

Remove subscriptions with `unsubscribe` or its `unsub` alias:

```text
unsubscribe invoices notifications
```

Each received message is printed with its sequence number and topic, then acknowledged by the sample handler.

## Produce Messages

The required `--topic` option selects the destination. Every positional argument is published as a separate UTF-8 message:

```text
produce --topic:orders "order #1001"
produce --topic:orders first second third
```

Use `--round:<count>` to publish every supplied message repeatedly. `send` is an alias for `produce`:

```text
produce --topic:orders --round:3 hello
send --topic:notifications --round:2 alpha beta
```

The sample prefixes each payload with its one-based round number and prints the broker identifier and total elapsed time.

## Inspect and Reset

```text
info
reset
close
```

`info` displays the queue and active topic subscriptions. `reset` clears only the received-message counter. `close` disposes the queue; exit the sample afterward or restart it before issuing more messaging commands.

## Suggested Scenario

Run these commands in order:

```text
subscribe demo
produce --topic:demo --round:3 "Hello RabbitMQ"
info
unsubscribe demo
```

You should observe three received messages; the handler acknowledges each one after printing it. The `demo` subscription should no longer appear after the final command.

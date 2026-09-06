# Zongsoft.Messaging.RabbitMQ 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

本交互式范例演示 `Zongsoft.Messaging.RabbitMQ` 的队列、消息发布、订阅和消息确认 API。

项目面向 .NET 10，默认连接 `127.0.0.1:5672` 的 RabbitMQ，用户名为 `program`、密码为 `xxxxxx`。运行前请修改 `Program.cs` 中的连接参数，使其与本地代理一致。

## 运行

在仓库根目录运行：

```shell
dotnet run --project messaging/rabbit/samples/Zongsoft.Messaging.RabbitMQ.Samples.csproj
```

## 订阅与取消订阅

将一个或多个主题作为参数即可订阅，`sub` 是 `subscribe` 的别名：

```text
subscribe orders invoices
sub notifications
```

使用 `unsubscribe` 或其别名 `unsub` 取消订阅：

```text
unsubscribe invoices notifications
```

范例处理器会输出每条已接收消息的序号和主题，然后确认该消息。

## 发布消息

必须通过 `--topic` 选项指定目标主题，每个位置参数都会作为一条独立的 UTF-8 消息发布：

```text
produce --topic:orders "order #1001"
produce --topic:orders first second third
```

使用 `--round:<次数>` 可重复发布全部输入消息，`send` 是 `produce` 的别名：

```text
produce --topic:orders --round:3 hello
send --topic:notifications --round:2 alpha beta
```

范例会在消息正文前添加从 1 开始的轮次编号，并输出代理返回的标识和总耗时。

## 查看与重置

```text
info
reset
close
```

`info` 显示队列及当前主题订阅；`reset` 只清空已接收消息计数；`close` 释放队列，执行后应退出范例，或重启程序后再执行消息命令。

## 建议场景

逐条执行：发布前等待订阅就绪，取消订阅前等待接收输出，不要整段连续粘贴：

```text
subscribe demo
produce --topic:demo --round:3 "Hello RabbitMQ"
info
unsubscribe demo
```

应观察到三条接收消息，处理器会在输出后确认每条消息；最后一条命令执行后，`demo` 不再出现在订阅列表中。

## 边界与清理

[Program.cs](Program.cs) 是自行持有队列的独立诊断程序；应用应采用[类库说明](../README.zh-Hans.md)中的插件部署与公共提供者方式。收到三条消息是隔离主题、无其他生产者且订阅已就绪时的预期，不代表任意网络条件下恰好一次投递。处理器调用确认 API 也不等同于业务事务提交；持久性与确认语义以驱动说明为准。

🚨 使用专用本地代理和唯一测试主题，不要消费生产主题。完成后取消本次订阅，执行 `close`，再执行 `exit` 并确认。客户端退出不会替你删除代理端的主题、队列、消费者状态或持久消息；如需清理，只通过代理管理工具处理本次创建的测试资源，不要清空共享代理。

# Zongsoft.Diagnostics.Protocols.Server 范例

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

本范例是 OpenTelemetry gRPC 服务端集成的插件。它在 `/Workbench/Diagnostics/Telemetry/Listener/Metrics` 注册 `MetricHandler`，将接收到的每组指标数据输出到宿主终端。

与可执行范例不同，本项目必须由已经托管 `Zongsoft.Diagnostics.Protocols.Server` 的 Zongsoft 应用加载。

## 构建与加载

在仓库根目录构建范例：

```shell
dotnet build Zongsoft.Diagnostics/protocols/server/samples/Zongsoft.Diagnostics.Protocols.Server.Samples.csproj
```

部署生成的程序集和 `Zongsoft.Diagnostics.Protocols.Server.Samples.plugin`，并在 `Zongsoft.Diagnostics.Protocols.Server` 依赖之后将范例插件加入宿主。随包的 `.deploy` 文件列出了插件部署所需的构件。

## 验证

1. 启动已加载诊断协议服务端和本范例插件的 Zongsoft 宿主；
2. 配置 OpenTelemetry 客户端或采集器，将指标导出到宿主的 OTLP gRPC 端点；
3. 产生应用指标；
4. 确认 `MetricHandler` 将每个指标计量器及其内容写入终端。

如果没有输出，请检查 OTLP 端点、传输安全设置、服务端插件是否启用，以及 `/Workbench/Diagnostics/Telemetry/Listener/Metrics` 注册路径。

## 背景与关键代码

OTLP 是 OpenTelemetry 的传输协议。本样例消费已经转换好的框架指标集合，而不是原始 protobuf 字节。[MetricHandler.cs](MetricHandler.cs) 继承类型化 HandlerBase 并打印条目，不包含数据库存储或重试队列。

宿主必须支持 HTTP/2 上的 gRPC。使用本地测试端点及不含用户标识的合成指标。代码生成依赖已检出的 proto 子模块，不能修改其内容。

## 预期范围与清理

打印指标只证明此次请求到达样例处理器，不证明所有指标类型/资源都被无损转换，也不证明已持久化。评估接收器前请阅读[服务端限制](../README.zh-Hans.md)。

测试后先停止导出器，再停止宿主；不再需要时只从测试宿主插件目录移除本样例程序集和清单。保护重定向控制台输出，没有样例自有数据库需要清理。

💡 无控制台输出也可能是没有产生匹配指标。先显式记录一个合成测量值，再排查批次。

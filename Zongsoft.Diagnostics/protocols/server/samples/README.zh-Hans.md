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

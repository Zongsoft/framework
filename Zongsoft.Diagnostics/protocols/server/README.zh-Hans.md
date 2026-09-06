# Zongsoft.Diagnostics.Protocols.Server 服务端插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Server)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Server)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**S**erver](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/server) 是基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 的 [_**g**RPC_](https://grpc.io) 诊断遥测服务端协议包。

它实现 OTLP 日志、指标与追踪采集服务，把生成式 Protobuf 消息转换成 Zongsoft 遥测模型，再把每个已接收批次分发给注册的 `IHandler` 实现。

## 安装与端点

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Server
```

请部署插件与选项产物。随包选项在 `http://*:4317` 定义名为 `telemetry` 的 HTTP/2 Kestrel 端点，即约定的 OTLP/gRPC 端口。绑定、TLS 或网络策略不同时，请在部署配置中覆盖该端点。

🚨 默认的通配明文监听只适用于可信开发网络。生产部署应明确绑定地址、启用 TLS 或由可信代理终止 TLS、验证发送方，并用防火墙限制端点。

## 处理模型

| OTLP 信号 | 服务单例 | 处理器参数 |
| --- | --- | --- |
| 日志 | `Listener.Logs` | 转换后的 Zongsoft 日志集合 |
| 指标 | `Listener.Metrics` | `IEnumerable<Meter>` |
| 追踪 | `Listener.Traces` | 转换后的追踪集合 |

通常通过插件树把处理器注册到对应 Processor 的 `Handlers` 集合。每个批次使用 `Parallel.ForEachAsync` 并发分发；单个处理器异常会被记录并隔离，不阻断其他处理器，请求取消信号会继续传递。

```xml
<extension path="/Workbench/Diagnostics/Telemetry/Listener/Metrics">
	<object type="MyCompany.Telemetry.MetricHandler, MyCompany.Telemetry" />
</extension>
```

具体 `HandlerBase<IEnumerable<Meter>>` 实现与部署步骤请参阅[范例插件](samples/README.zh-Hans.md)。

## 运行限制

当前 Processor 在分发后返回空的成功 OTLP 响应，并记录处理器失败。需要可靠接收时，处理器必须在返回前显式入队或持久化，并定义自己的过载策略。请限制队列和执行时间；慢处理器会延长导出调用，不受控的并行工作会耗尽资源。

时间戳从 OTLP Unix 纳秒转换为框架模型时保留到毫秒。请对照固定版本的 [OTLP 规范](https://opentelemetry.io/docs/specs/otlp/)验证属性类型、直方图/摘要语义和可接受的数据损失。

## 延伸阅读

- [诊断概述](../../README.zh-Hans.md)
- [客户端协议包](../client/README.zh-Hans.md)
- [服务端范例](samples/README.zh-Hans.md)
- [OTLP 导出器配置](https://opentelemetry.io/docs/languages/sdk-configuration/otlp-exporter/)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

使用 Web 宿主，并显式部署 `Zongsoft.Web.Grpc`。在 `/Workbench/Diagnostics/Telemetry/Listener` 下注册处理器并配置 HTTP/2 端点；协议 DLL 本身不会启动接收器。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Diagnostics.Protocols.Server` | [Zongsoft.Diagnostics.Protocols.Server.plugin](src/Zongsoft.Diagnostics.Protocols.Server.plugin) |
| 文件复制及依赖 | [Zongsoft.Diagnostics.Protocols.Server.deploy](src/Zongsoft.Diagnostics.Protocols.Server.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft web grpc]
nuget:Zongsoft.Web.Grpc

[plugins zongsoft diagnostics protocols server]
nuget:Zongsoft.Diagnostics.Protocols.Server
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Diagnostics.Protocols.Server.option`、`Zongsoft.Diagnostics.Protocols.Server.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

# Zongsoft.Diagnostics.Protocols.Client 客户端插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Client)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Client)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**C**lient](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/client) 是基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 诊断遥测的 [_**g**RPC_](https://grpc.io) 客户端协议包。

本包包含 OTLP C# 消息类型，以及日志、指标和追踪采集服务的生成式 gRPC 客户端。它是协议程序集：不会采集遥测、批处理、重试导出，也不会配置 OpenTelemetry SDK 管线。

## 安装与范围

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Client
```

直接调用 OTLP 采集服务，或其他组件需要生成式协议类型时使用本包。普通应用插桩应优先采用[诊断总指南](../../README.zh-Hans.md)所述的官方 OpenTelemetry .NET SDK。

构建会为导入的 Schema 生成消息，并为 Collector 的 `*_service.proto` 生成客户端 Stub。主要命名空间是 `OpenTelemetry.Proto.Collector.Logs.V1`、`.Metrics.V1`、`.Trace.V1` 及其数据消息命名空间。

## 直接调用模式

```csharp
using Grpc.Net.Client;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using Zongsoft.Diagnostics.Protocols.Client;

using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
var address = new Uri("https://collector.example.invalid/opentelemetry");
using var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
{
	HttpHandler = new GrpcClientHandler(address.AbsolutePath, new SocketsHttpHandler()),
	DisposeHttpClient = true,
});
var client = new MetricsService.MetricsServiceClient(channel);
var response = await client.ExportAsync(
	new ExportMetricsServiceRequest(),
	cancellationToken: cancellation.Token);
```

`GrpcClientHandler` 将声明的基路径加到生成的 RPC 路径前。路径必须与接收端一致，随附服务端使用 `/opentelemetry`。原生 `GrpcChannel` 不会应用地址中的路径，参见[官方子目录调用说明](https://learn.microsoft.com/aspnet/core/grpc/troubleshoot#calling-grpc-services-hosted-in-a-sub-directory)。

处理器接受 `/` 或以斜杠开头的字面路径，路径段允许 ASCII 字母、数字和 `-._~`。允许一个尾斜杠；中间空段、独立的 `.`、`..` 段、编码字符、查询及片段均无效。`Path` 保留声明原文，连接原生 RPC 路径时只使用一个斜杠。每条处理链只允许安装一个本处理器，每个请求只应用一次前缀。它保留 authority、转义后的请求路径和查询、请求头、消息内容、HTTP 版本、取消信号及响应，不缓冲消息、不选择传输协议，适用于单次及流式调用。

调用方提供内部传输处理器及其代理、TLS、HTTP/2 设置。释放 `GrpcClientHandler` 会释放内部处理器；上例通过 `DisposeHttpClient = true` 交给通道释放。共享处理器或 HTTP 客户端时由调用方持有，并在所有通道停止后释放，详见[客户端选项](https://learn.microsoft.com/aspnet/core/grpc/configuration#configure-client-options)。

官方 OpenTelemetry 导出器已经会向 `Endpoint` 追加 RPC 路径，应直接配置完整端点，例如 `http://localhost:4317/opentelemetry`，不要在该管线再次安装 `GrpcClientHandler`。本处理器用于直接调用生成式客户端。

请按照 [OTLP 规范](https://opentelemetry.io/docs/specs/otlp/)填充消息。空请求只演示调用形状；实用导出器必须正确归属遥测并处理部分成功。

🚨 不要关闭 TLS 验证或硬编码凭据。请配置截止时间、消息大小、代理、重试/批处理与背压，并始终传递取消信号。

生成类型对应仓库锁定的协议版本。Protobuf 可能保持线协议兼容，但生成式源码 API 仍会变化；不要把这些消息当作应用自有的长期持久化格式。

## 本地验证

从仓库根目录运行：

```shell
dotnet build Zongsoft.Core/src/Zongsoft.Core.csproj -p:GeneratePackageOnBuild=false
dotnet test Zongsoft.Diagnostics/protocols/client/test/unit/Zongsoft.Diagnostics.Protocols.Client.Tests.csproj -p:GeneratePackageOnBuild=false
```

单元测试通过公开发送接口和内存处理器检查基路径校验、重复安装、查询串和请求对象保留。单元测试项目不依赖 ASP.NET Core，也不启动服务。

独立的集成测试项目使用本地 HTTP/2 服务检查一元调用、三种流式调用、大消息、元数据、状态、trailers、取消与 deadline，以及通道对 HttpHandler/HttpClient 的释放约定。需要时显式运行：

```shell
dotnet test Zongsoft.Diagnostics/protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx -p:GeneratePackageOnBuild=false
```

ASP.NET Core 和测试专用的 gRPC 服务仅由集成测试项目引用。客户端库本身不引用 Web 托管项目；普通客户端解决方案和 Cake `test` 任务不包含集成测试。

## 延伸阅读

- [诊断概述](../../README.zh-Hans.md)
- [服务端包](../server/README.zh-Hans.md)
- [OTLP 规范](https://opentelemetry.io/docs/specs/otlp/)
- [gRPC .NET 客户端指南](https://learn.microsoft.com/zh-cn/aspnet/core/grpc/client)

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../../../Zongsoft.Plugins/README.zh-Hans.md)。

清单让生成的协议程序集可用，不会自动创建导出器或发送遥测，仍需按前文创建和配置客户端。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Diagnostics.Protocols.Client` | [Zongsoft.Diagnostics.Protocols.Client.plugin](src/Zongsoft.Diagnostics.Protocols.Client.plugin) |
| 文件复制及依赖 | [Zongsoft.Diagnostics.Protocols.Client.deploy](src/Zongsoft.Diagnostics.Protocols.Client.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft diagnostics protocols client]
nuget:Zongsoft.Diagnostics.Protocols.Client
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Diagnostics.Protocols.Client.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

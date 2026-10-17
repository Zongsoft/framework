# Zongsoft.Diagnostics.Protocols.Server 服务端插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics.Protocols.Server)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics.Protocols.Server)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

[**Z**ongsoft.**D**iagnostics.**P**rotocols.**S**erver](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics/protocols/server) 是基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 的 [_**g**RPC_](https://grpc.io) 诊断遥测服务端协议包。

它实现 OTLP 日志、指标与追踪采集服务，并向注册的 `IHandler` 分发收到的请求。指标转换成框架计量器集合；日志和追踪目前通过 `Parameters` 提供原始请求，处理器的普通参数为 null。

## 安装与端点

```shell
dotnet add package Zongsoft.Diagnostics.Protocols.Server
```

请部署插件与选项产物。随包选项声明 `telemetry` 地址为 `http://*:4317/opentelemetry`，协议为 `grpc+http2`；三个具体处理类均标注 `[Handler("/opentelemetry")]`。Plugins.Web 将完整 URL 投影为 Kestrel 的物理监听，Web.Grpc 将三类服务挂载到该路径。处理类与端点的路径必须一致，修改部署地址不会改变 Handler 注解。

🚨 默认的通配明文监听只适用于可信开发网络。生产部署应明确绑定地址、启用 TLS 或由可信代理终止 TLS、验证发送方，并用防火墙限制端点。

源配置见 [Zongsoft.Diagnostics.Protocols.Server.option](src/Zongsoft.Diagnostics.Protocols.Server.option)：

```xml
<?zongsoft.tools.binding name="telemetry"
	path="Kestrel/Endpoints/telemetry"
	template="${this.Url}"
	protocols="${this.Protocols}"
	required="true"?>
<options>
	<option path="kestrel">
		<endpoints>
			<telemetry url="http://*:4317/opentelemetry" protocols="grpc+http2" />
		</endpoints>
	</option>
</options>
```

官方 OpenTelemetry 导出器将 Endpoint 设为完整地址即可；直接使用生成式客户端时配置 [GrpcClientHandler](../client/README.zh-Hans.md#直接调用模式)。两者最终发送 `/opentelemetry/<原生服务名>/<方法名>`，代理应保留完整路径。反射服务也挂载于该区域；不带前缀或错误前缀的调用不会进入业务处理器，区域内未知 RPC 返回 UNIMPLEMENTED。

## 处理模型

| OTLP 信号 | 服务单例 | 处理器参数 |
| --- | --- | --- |
| 日志 | `Listener.Logs` | null；原始 `ExportLogsServiceRequest` 位于 `Parameters` |
| 指标 | `Listener.Metrics` | `IEnumerable<Meter>` |
| 追踪 | `Listener.Traces` | null；原始 `ExportTraceServiceRequest` 位于 `Parameters` |

通常通过插件树把处理器注册到对应 Processor 的 `Handlers` 集合。每个批次使用 `Parallel.ForEachAsync` 并发分发；单个处理器异常会被记录并隔离，不阻断其他处理器，请求取消信号会继续传递。

以下摘自[实际样例清单](samples/Zongsoft.Diagnostics.Protocols.Server.Samples.plugin)。其 [MetricHandler](samples/MetricHandler.cs) 仅向终端输出指标；需部署样例程序集，不是持久化处理器。

```xml
<extension path="/Workbench/Diagnostics/Telemetry/Listener/Metrics">
	<object name="MetricHandler" type="Zongsoft.Diagnostics.Protocols.Server.Samples.MetricHandler, Zongsoft.Diagnostics.Protocols.Server.Samples" />
</extension>
```

具体 `HandlerBase<IEnumerable<Meter>>` 实现与部署步骤请参阅[范例插件](samples/README.zh-Hans.md)。

## 运行限制

当前 Processor 在分发后返回空的成功 OTLP 响应，并记录处理器失败。需要可靠接收时，处理器必须在返回前显式入队或持久化，并定义自己的过载策略。请限制队列和执行时间；慢处理器会延长导出调用，不受控的并行工作会耗尽资源。

时间戳从 OTLP Unix 纳秒转换为框架模型时保留到毫秒。请对照固定版本的 [OTLP 规范](https://opentelemetry.io/docs/specs/otlp/)验证属性类型、直方图/摘要语义和可接受的数据损失。

## 本地验证

从仓库根目录运行：

```shell
dotnet test Zongsoft.Diagnostics/protocols/server/test/Zongsoft.Diagnostics.Protocols.Server.Tests.csproj -p:GeneratePackageOnBuild=false
```

测试在隔离子进程内通过真实 `Application.Web` 加载插件、选项及现有 samples，自动投影监听并挂载服务。它检查官方 SDK 和生成式客户端的三信号载荷、样例指标输出、区域反射、错误路径、取消传播及停止后端口释放。所有网络通信限定在本机回环地址；此验证不包含代理或生产宿主部署。

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

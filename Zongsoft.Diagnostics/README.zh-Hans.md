# Zongsoft.Diagnostics 诊断插件库

![License](https://img.shields.io/github/license/Zongsoft/framework)
![NuGet Version](https://img.shields.io/nuget/v/Zongsoft.Diagnostics)
![NuGet Downloads](https://img.shields.io/nuget/dt/Zongsoft.Diagnostics)
![GitHub Stars](https://img.shields.io/github/stars/Zongsoft/framework?style=social)

[English](README.md) |
[简体中文](README.zh-Hans.md)

-----

## 概述

[**Z**ongsoft.**D**iagnostics](https://github.com/Zongsoft/framework/tree/main/Zongsoft.Diagnostics) 是 [_**Z**ongsoft_](https://github.com/Zongsoft/framework) 开源框架的诊断插件库，提供了基于 [_**O**pen**T**elemetry_](https://opentelemetry.io) 诊断遥测的功能集。

通过 [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) 配置文件默认定义了 _**O**pen**T**elemetry_ 标准的指标 _(Metric)_、跟踪 _(Trace)_ 导出器。

使用 `driver="opentelemetry"` 导出 OTLP 数据。将 `server` 设置为接收端点，`protocol` 设置为 `grpc` 或 `HttpProtobuf`；接收端点必须支持所选的 OTLP 传输协议。随附 Diagnostics 接收端使用 `/opentelemetry`，配套导出器配置为 `server=http://localhost:4317/opentelemetry;protocol=grpc`。官方 SDK 导出器直接使用完整地址；生成式客户端使用[客户端基路径处理器](protocols/client/README.zh-Hans.md#直接调用模式)。

## 启用或禁用导出器

代码入口统一为 `Zongsoft.Diagnostics.Telemetry.ExporterLauncher.Default`（OTLP）和 `ExporterLauncher.Console`，两者均提供 `Meters`、`Tracers`、`Logs` 启动器实例。宿主通过程序集扫描发现它们；配置使用 `driver="opentelemetry"` 或 `driver="opentelemetry.console"`。[ExporterLauncher.cs](src/Telemetry/ExporterLauncher.cs) 中的私有注册类负责自动接入日志。

指标、追踪的每个导出器都支持 `enabled="true|false"`；省略时默认为 `true`，兼容已有配置。`enabled="false"` 的导出器在解析驱动、创建提供程序前被排除，不要求注册对应驱动，也不解析其连接设置。某一信号的导出器全部禁用时，不创建该信号的提供程序；XML 和布尔值本身仍须合法。

例如，Web 配置可以保留公共 OTLP 声明并关闭其指标导出：

```xml
<exporter exporter.name="telemetry" driver="opentelemetry" enabled="false"
	settings="server=http://127.0.0.1:4317;protocol=grpc;" />
```

指标与追踪的开关相互独立；Web 插件仍可向同一指标配置添加启用的 Prometheus 导出器。

## 强类型导出设置

`settings` 仍使用分号分隔的文本，`FilteringExporter.Settings` 和 `IExporterLauncher.Launch` 的字符串契约保持不变。启动器内部通过 [ExporterLauncherSettingsDriver](src/Configuration/ExporterLauncherSettingsDriver.Default.cs) 创建强类型对象，SDK 选项转换集中在 `Telemetry/ExporterLauncher.cs`。

| 驱动名称 | 设置类型 | 驱动入口 |
| --- | --- | --- |
| `opentelemetry` | `ExporterLauncherSettings` | `ExporterLauncherSettingsDriver.Default` |
| `opentelemetry.console` | `ExporterLauncherSettings.Console` | `ExporterLauncherSettingsDriver.Console` |

两个设置类型位于 `Zongsoft.Diagnostics.Configuration`，分别继承连接设置基类；Console 不继承 OTLP 属性。私有驱动通过公开的强类型接口提供实例，在 `.plugin` 的 `/Workbench/Configuration/ConnectionSettings/Drivers` 下显式注册，不依赖名为 `Instance` 的自动发现约定。直接使用下面的 API 不需要插件宿主：

```csharp
var settings = ExporterLauncherSettingsDriver.Default.GetSettings(
	"server=http://localhost:4317;protocol=grpc;timeout=30s;interval=5s;");
var consoleSettings = ExporterLauncherSettingsDriver.Console.GetSettings(
	"target=Console,Debug;interval=10s;");
```

| 参数 | 强类型 | 缺省值与用途 |
| --- | --- | --- |
| `server` | `string` | OTLP 地址；未声明时按协议选择 `http://localhost:4317` 或 `http://localhost:4318` |
| `protocol` | `OtlpExportProtocol` | OTLP：`Grpc`；也支持 `HttpProtobuf` |
| `processorType` | `ExportProcessorType` | OTLP：`Batch`；也支持 `Simple`，控制日志、跟踪处理器 |
| `timeout` | `TimeSpan` | `30s`；OTLP 导出及批处理超时，Console 指标读取超时 |
| `interval` | `TimeSpan` | `10s`；指标周期及 OTLP 批处理延迟 |
| `maxBatchSize` | `int` | OTLP 批处理：`512` |
| `maxQueueSize` | `int` | OTLP 批处理：`2048` |
| `target` | `ConsoleExporterOutputTargets` | Console：`Console`；可选 `Debug` 或 `Console,Debug` |

参数名忽略大小写，时间支持 `1500ms`、`5s` 等连接设置格式；类型化默认值不会写回原始 `Value`。未知参数沿用连接设置基类的行为，保留在字典中，不据此新增导出行为。

**空或未提供的整个 `settings` 不覆盖 SDK 默认值及环境变量。** 上表默认值用于非空设置文本中省略的参数。启用的导出器读取到无效类型时会报错，不再悄悄退回默认值；选项转换还校验枚举、HTTP/HTTPS 绝对地址、时间范围（1～2147483647 毫秒），以及正数批量/队列大小和 `maxBatchSize <= maxQueueSize`。显式空 `server` 无效。OTLP 沿用现有转换路径，三种信号都会读取处理器和批处理设置；这些设置仅对日志、跟踪的处理器生效。Console 的日志和跟踪只读取 `target`，不读取指标专用的时间参数。禁用的导出器不解析设置。

新配置统一使用 `opentelemetry` 和 `opentelemetry.console`；启动器仍兼容 `telemetry`、`OpenTelemetry`、`Console`（忽略大小写），OTLP 也保留空驱动名匹配。`exporter.name="telemetry"` 是配置项名称，与驱动名称不同；既有 `diagnostics.telemetry`、`diagnostics.logging` 宿主变量及开关不改名。

## Microsoft ILogger 日志

插件在服务注册阶段读取 `/Diagnostics/Diagnostor/Logs`，自动将 `Microsoft.Extensions.Logging.ILogger` 接入 OpenTelemetry。宿主无需修改 `Program.cs`，也不需要 ASP.NET Core。Zongsoft 自有 `Logging`/`ILogger` 和 `Console.WriteLine` 不在此管线中。

日志默认关闭。启用时将以下节点放入 `<diagnostor>`：

```xml
<logs includeScopes="true" includeFormattedMessage="true">
	<exporters>
		<exporter exporter.name="telemetry" driver="opentelemetry" enabled="true"
			settings="server=http://127.0.0.1:4317;protocol=grpc;processorType=batch;timeout=30s;interval=5s;" />
	</exporters>
</logs>
```

本地调试也可以使用 Console 日志导出器，将以下节点放入 `<logs><exporters>`，可以与 OTLP 导出器并存：

```xml
<exporter exporter.name="console" driver="opentelemetry.console" enabled="true" settings="target=Console;" />
```

`target` 沿用指标、跟踪 Console 导出器的配置，默认为 `Console`，也支持 `Debug` 或 `Console,Debug`。日志逐条输出，不使用 `interval`、`timeout` 或批处理参数。这是 OpenTelemetry 的详细日志输出；若宿主同时启用了标准 Console 日志提供程序，同一条日志可能显示两次。

日志开关独立于指标和跟踪；缺少 `<logs>`、没有导出器或全部 `enabled="false"` 时不注册日志提供程序。禁用项不解析驱动和设置；已启用但缺少驱动时启动失败，异常 Data 包含 `Signal=logs` 和 `ExporterDriver`。

日志类别及级别沿用宿主标准 `Logging:LogLevel`，支持 `Logging:OpenTelemetry:LogLevel` 的提供程序专用规则，不另设 `<filter>`。`includeScopes` 和 `includeFormattedMessage` 默认 true；结构化字段、异常及当前 Activity 的 TraceId/SpanId 由 SDK 导出。无 Activity 的启动日志通常没有跟踪标识。

日志资源使用 `IHostEnvironment.ApplicationName`（没有宿主环境时使用当前应用域名称），支持 `OTEL_SERVICE_NAME`、`OTEL_RESOURCE_ATTRIBUTES` 覆盖；同一进程与指标、跟踪共享默认 `service.instance.id`。Provider 由宿主 DI 容器持有，正常释放时刷新批次并释放导出器，不由指标/跟踪工作器启停。配置变更需重启宿主，不补发注册前的日志。现有 OpenTelemetry 依赖已包含日志能力，插件声明和部署依赖无需增加 ASP.NET Core。

接收端须支持 OTLP 日志。Aspire Dashboard 可直接接收；现有仅指标的 Collector 示例需要另外配置 logs 管线及日志后端，Prometheus 不存储日志。

## 普通宿主与 Web 宿主

通用库不依赖 ASP.NET Core。daemon、terminal 仅部署本包，使用 OTLP 向 Collector 发送指标，由 Prometheus 抓取 Collector。Web 直接抓取端点已迁至 [Zongsoft.Diagnostics.Web](../Zongsoft.Diagnostics.Web/README.zh-Hans.md)，只部署到 Web 宿主。

Collector 接收端通常使用 `server=http://127.0.0.1:4317;protocol=grpc`，不能沿用 Zongsoft 接收器的 `/opentelemetry` 前缀。Collector 与 Prometheus 的 YAML 配置文件由各自服务读取；容器内应使用服务名并绑定容器可达地址。

工作器将应用名写入 `service.name`，SDK 为进程生成 `service.instance.id`；指标与追踪共享身份。`OTEL_SERVICE_NAME`、`OTEL_RESOURCE_ATTRIBUTES` 可覆盖默认值。已启用的导出器缺少对应驱动时，启动抛出 InvalidOperationException，Data 包含 ExporterDriver、Signal；启动中途失败会释放已创建的提供程序。

通用库定向测试：`dotnet test test/Zongsoft.Diagnostics.Tests.csproj -p:GeneratePackageOnBuild=false`。先构建 Core，测试覆盖纯 .NET 运行时、导出器注册、资源身份、指标/追踪输出及生命周期。

`Zongsoft.Diagnostics.slnx` 和默认 Cake `test` 任务只包含单元测试。协议集成测试分别位于 `protocols/client/test/integration`、`protocols/server/test/integration`，各自的解决方案与集成测试项目同目录：[客户端](protocols/client/test/integration/Zongsoft.Diagnostics.Protocols.Client.Tests.slnx)、[服务端](protocols/server/test/integration/Zongsoft.Diagnostics.Protocols.Server.Tests.slnx)。按[本地验证](protocols/server/README.zh-Hans.md#本地验证)准备依赖后，在本目录显式执行 `dotnet cake build.cake --target=integration-test`，验证本地 HTTP/2 和 OTLP 服务。

## 内置 OpenTelemetry 协议

[`proto`](proto) 目录是指向上游 [OpenTelemetry Protocol 仓库](https://github.com/open-telemetry/opentelemetry-proto)的 Git 子模块，其中包含 [OTLP 协议规范](proto/docs/specification.md)以及对应的语言无关接口类型（[`.proto` 文件](proto/opentelemetry/proto)）。该子模块必须保持干净；协议说明和本项目专用指引应维护在本 README 中，不得写入上游工作树。

### 语言无关接口类型

Proto 文件可以作为 Git 子模块使用，也可以复制到使用方项目中直接构建。OpenTelemetry 的各语言客户端库会把编译产物发布到 Maven 等中央仓库。修改上游定义前必须遵循 [OpenTelemetry Proto 贡献指南](proto/CONTRIBUTING.md)。

### OTLP/JSON

OTLP/JSON 在线路表示方面的附加要求参见 [JSON Protobuf 编码规范](https://github.com/open-telemetry/opentelemetry-specification/blob/main/specification/protocol/otlp.md#json-protobuf-encoding)。

### 生成 gRPC 客户端库

上游仓库使用 `make gen-${LANGUAGE}` 生成原始 gRPC 客户端库，目前支持：

- cpp
- csharp
- go
- java
- objc
- openapi（Swagger）
- php
- python
- ruby

### 成熟度

从 1.0.0 开始，发行版仍可能包含下表中标记为不稳定（alpha 或 beta）的组件。

| 组件 | Binary Protobuf 成熟度 | JSON 成熟度 |
| --- | --- | --- |
| common/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| resource/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| metrics/\*<br>collector/metrics/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| trace/\*<br>collector/trace/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| logs/\*<br>collector/logs/* | Stable | [Stable](proto/docs/specification.md#json-protobuf-encoding) |
| profiles/\*<br>collector/profiles/* | Development | [Development](proto/docs/specification.md#json-protobuf-encoding) |

成熟度定义参见[版本与稳定性](https://github.com/open-telemetry/opentelemetry-specification/blob/a08d1f92f62acd4aafe4dfaa04ae7bf28600d49e/specification/versioning-and-stability.md)。

### 稳定性保证

标记为 `Stable` 的组件保证已有字段类型、编号和名称，服务及包名，方法名、参数、返回类型和调用种类，消息与枚举符号，包名与目录结构，以及现有 `optional` 或 `repeated` 声明不会发生不兼容变更，也不会删除已有符号。

允许以下兼容性增量变更：

- 向现有消息新增字段。
- 新增消息或枚举。
- 新增枚举选项。
- 向现有 `oneof` 字段新增选项。
- 新增服务。
- 向现有服务新增方法。

所有增量变更都必须说明分别实现变更前后协议版本的发送方与接收方如何互操作。

### 实验

全新实验组件应隔离在 `development` 子目录中，并使用 `Development`、`Alpha`、`Beta` 或 `Release Candidate` 成熟度标识。未被稳定组件引用的实验组件可以在实验结束时删除；成功的实验必须经过审查后才能标记为 `Stable`，并自此遵守完整的稳定性保证。

稳定组件中新增的实验字段或消息必须保持兼容，并明确标记为非稳定状态。如果实验失败，这些字段或消息仍须保留，只能标记为弃用；发送方通常应将其留空，接收方必须持续容忍空值。

### 生成代码

上游项目不保证任何特定 Proto 代码生成器所生成代码的稳定性。

### 上游维护者与批准者

- 维护者：[OpenTelemetry Technical Committee](https://github.com/open-telemetry/community/blob/main/community-members.md#technical-committee)
- 批准者：[OpenTelemetry Specification Sponsors](https://github.com/open-telemetry/community/blob/main/community-members.md#specifications-and-proto)

角色说明参见 OpenTelemetry 社区的[成员指南](https://github.com/open-telemetry/community/blob/main/guides/contributor/membership.md#maintainer)。

## 插件化接入

优先通过宿主组合本能力；包引用用于编译，而插件加载还需要部署清单和运行产物。完整流程见[插件化入门](../Zongsoft.Plugins/README.zh-Hans.md)。

插件贡献诊断配置及启动组件。通过同主文件名 `.option` 选择过滤器和导出器；加载后可能启用遥测导出，只使用受控目标。

| 运行产物 | 源码依据 |
| --- | --- |
| `Zongsoft.Diagnostics` | [Zongsoft.Diagnostics.plugin](src/Zongsoft.Diagnostics.plugin) |
| 文件复制及依赖 | [Zongsoft.Diagnostics.deploy](src/Zongsoft.Diagnostics.deploy) |

在已有宿主的 `.deploy` 中加入以下片段（保留宿主原有 Main 等基础清单，不要用片段覆盖整份文件）：

```ini
[plugins zongsoft diagnostics]
nuget:Zongsoft.Diagnostics
```

按入门指南在测试部署目录执行 `dotnet deploy`，指定匹配宿主的 `framework`、`platform`、`architecture`，并按需指定 `site`。实际部署应固定兼容版本；片段没有列出的数据库、缓存、商业运行时等应用依赖仍需另外准备。

清单列出的附属产物包括：`Zongsoft.Diagnostics.option`、`Zongsoft.Diagnostics.plugin`。同时保留程序集、依赖与附属资源目录。部署后重启宿主，先检查插件加载与服务/驱动注册，再验证前文的使用流程；不要把“文件已复制”当作“功能已启用”。

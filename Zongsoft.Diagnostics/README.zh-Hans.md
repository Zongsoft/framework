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

通过 [Zongsoft.Diagnostics.option](src/Zongsoft.Diagnostics.option) 配置文件默认定义了 _**O**pen**T**elemetry_ 标准的指标 _(Metric)_、跟踪 _(Trace)_ 导出器，以及 [_**P**rometheus_](https://prometheus.io) _指标_ 导出器。

使用 `driver="telemetry"` 导出 OTLP 数据。将 `server` 设置为接收端点，`protocol` 设置为 `grpc` 或 `HttpProtobuf`；接收端点必须支持所选的 OTLP 传输协议。随附 Diagnostics 接收端使用 `/opentelemetry`，配套导出器配置为 `server=http://localhost:4317/opentelemetry;protocol=grpc`。官方 SDK 导出器直接使用完整地址；生成式客户端使用[客户端基路径处理器](protocols/client/README.zh-Hans.md#直接调用模式)。

## Prometheus 监听

`prometheus` 导出器接受 HTTP 抓取请求。`urls` 保存包含抓取路径的完整监听地址，尾斜杠可省略，绑定声明读取同一份地址，并用常量模板声明本启动器固定的 HTTP/1 能力：

```xml
<!-- Zongsoft.Diagnostics/src/Zongsoft.Diagnostics.option；只列监听相关配置 -->
<?zongsoft.tools.binding name="prometheus"
	path="Diagnostics/Diagnostor/Meters/Exporters/*"
	filter="Driver:prometheus"
	template="${option.Settings['urls']}"
	protocols="http1"?>
<options>
	<option path="/Diagnostics">
		<diagnostor>
			<meters>
				<filter>*</filter>
				<exporters>
					<exporter exporter.name="prometheus" driver="prometheus"
						settings="urls=http://127.0.0.1:9464/metrics,http://localhost:9464/metrics;totalSuffix=true;cacheDuration=300ms;" />
				</exporters>
			</meters>
		</diagnostor>
	</option>
</options>
```

`urls` 必填；省略 `protocol` 时默认为 `http1`，显式指定时只接受忽略大小写的单个 `http1`，例如 `urls=http://127.0.0.1:9464/metrics;protocol=http1`。空值、协议列表（包括重复的 `http1`）和 `protocols` 设置均无效。绑定使用常量模板 `protocols="http1"`，省略设置也不需要制作工具猜测默认协议；本启动器未实现 HTTP/2、HTTP/3 或应用层协议组合。这是运行配置校验，与制作工具的绑定映射检查职责独立。

每个 URL 使用 http 或 https，监听地址支持 IP、通配地址、localhost 及普通域名。同一导出器的基路径忽略大小写和可选尾斜杠，/metrics、/Metrics/ 等效，未指定路径表示根 /。声明路径段只允许 ASCII 字母、数字及 -._~，拒绝中间空段、独立点段、凭据、query、fragment 和编码路径。源配置保持原文，抓取选项去掉非根路径的尾斜杠。独立 Kestrel 服务器使用原始 URI 按路径段匹配，不解码或折叠点段后选择范围；自身及子路径均可抓取，/metrics-extra 返回 404，无重定向或双斜杠补偿。Windows 与 Linux 行为一致。

IP 声明保留网卡地址和地址族范围：`*`、`0.0.0.0` 只监听 IPv4，`[::]` 只监听 IPv6，`localhost` 监听可用的 IPv4/IPv6 回环地址。普通域名在启动时不做 DNS 解析，而是在可用的 IPv4/IPv6 通配地址上监听，按 HTTP Host 匹配声明的域名，忽略大小写及末尾点号；国际化域名与其 ASCII 形式等效。例如 `urls=http://metrics.example.com:9464/metrics` 接受 9464 端口上使用该域名的请求，调用方须将域名解析到本服务器。这是主机路由规则，不是身份认证。

同一导出器的多条声明可以共用物理监听，但每个请求仍须符合某条原始声明的 scheme、本地端口、网卡地址、地址族，以及普通域名声明的 Host 条件。因此，域名与 `127.0.0.1` 混用时，仅该回环地址接受任意 Host，其它本地地址仍要求声明的域名；不同域名、端口不会相互扩大访问范围。

地址以 `,`、`;` 或 `|` 分隔；中间空项和尾部分隔符均报错。在连接设置内部使用分号分隔地址时，将整个 `urls` 值加引号，例如 `urls='http://127.0.0.1:9464/metrics/;http://localhost:9464/metrics/';`。独立的 `path` 设置必须移除，即使值为空也会报错。缺少地址时不会回退到隐含监听。

`totalSuffix` 默认为 `true`，`cacheDuration` 默认为 `300ms`，二者应用于独立监听复用的 ASP.NET Core 导出器；负缓存时间按零处理，过大的值限制到所支持的毫秒范围。登记导出器选项不会自动添加 ASP.NET Core 路由。独立监听由诊断提供程序持有，并随其释放。

默认地址仅限回环访问。反向代理位于另一个容器时，必须显式配置该容器可达的监听地址，绑定工具不改写回环地址。独立服务器占用声明的端口，不能与其它服务器共享同一 socket。HTTPS 使用指标服务容器中 IConfiguration（缺省取 ApplicationContext.Current.Configuration）的 Kestrel:Certificates 配置；不继承宿主 Endpoints，也不使用 HTTP.sys 证书绑定。绑定指令表达暴露意图，本身不启动监听；出站 `telemetry` 导出器不参与该绑定。

定向验证位于 [test](test)。先执行 `dotnet build Zongsoft.Diagnostics.slnx -p:GeneratePackageOnBuild=false -p:ZongsoftCodeStyleStrict=true`，再执行 `dotnet test test/Zongsoft.Diagnostics.Tests.csproj --no-build`。测试检查公开导出器选项，并通过临时 HTTP/HTTPS 监听验证域名与 IP 混用、IPv4/IPv6 隔离、实际指标输出、证书选择、释放及启动失败清理。域名请求直接连接回环地址，无须 DNS 或 hosts 文件改动。不启动业务宿主，不代替反向代理及生产 HTTPS 部署验收。

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

# Web 端点与请求范围

[English](endpoints.md) | [简体中文](endpoints.zh-Hans.md)

`Zongsoft.Web.Hosting` 提供独立的 Kestrel 配置转换工具，以及不依赖服务器配置的逻辑端点集合和请求范围约束。独立 ASP.NET 宿主必须在配置加载完成后显式接入；仅引用程序集或添加 zongsoft.tools.binding 处理指令不会启动这套管线。Plugins.Web 通过 IServiceRegistration 扫描，在 Web 宿主存在 Kestrel:Endpoints 声明且未显式注册端点时自动接入转换、范围与监视，无需修改 hosting；gRPC 服务注册由 Web.Grpc 完成。

## 宿主接入

下列普通 HTTP 示例放在 ASP.NET Core WebApplicationBuilder 的配置完成阶段。示例中的 builder 是现有构建器；projection 的生命周期应覆盖应用运行及释放。

```csharp
using Zongsoft.Web.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using var projection = KestrelUtility.Convert(
	builder.Configuration.GetSection("Kestrel"), out var declarations);
var endpoints = builder.Services.AddWebEndpoints(declarations);

builder.WebHost.ConfigureKestrel(options => options.Configure(projection, reloadOnChange: false));
builder.Services.Configure<SocketTransportOptions>(options => KestrelUtility.Configure(options, endpoints));

var app = builder.Build();
app.UseWebEndpoints();
app.UseRouting();
// 身份验证、授权、静态文件及现有端点映射在此之后接入。
```

`KestrelUtility.Convert(configuration, out declarations, protocols)` 从一次输入生成独立的 ConfigurationRoot 和只读逻辑端点列表；简写规范化规则可以通过 protocols 传入。返回配置由调用方释放；原配置及逻辑端点不随其修改而变化。只转换时不注册服务、安装中间件、订阅重载或启动监听；没有端点时返回保留原生选项的独立配置。调用方指定配置节，工具不硬编码 Kestrel 根路径。

`AddWebEndpoints(declarations)` 从逻辑端点及已经注册的协议规则建立请求范围快照。也可以直接构造 WebEndpoint 和 WebEndpointCollection，完全不读取配置、不使用 Kestrel。Kestrel 投影须在原始端点加载之前应用。配置变更比较、重启提示与投影生命周期由宿主负责，集合不再提供 Configure 或 HasChanged。

`AddWebEndpoints(factory)` 在最终服务提供器中延迟创建集合，允许后续插件服务注册补全协议；该工厂只执行一次，不临时构建容器。`AddWebEndpointPipeline()` 通过私有启动过滤器在应用管线之前安装范围中间件，重复注册无副作用；也可自行选择下面的手动接入。

`UseWebEndpoints` 必须位于路由、转发头及可能提前返回的普通 HTTP 中间件之前，不可重复安装。它不代替身份验证和授权。范围约束只管监听及路由归属；任意自定义中间件仍由应用负责。静态文件及普通端点不能通过 gRPC 专用范围接收普通请求。

## 快照与投影

每个配置项提供完整 `Url` 和 `Protocols`，例如：

```ini
Kestrel:Endpoints:metrics:Url=http://127.0.0.1:4317/opentelemetry
Kestrel:Endpoints:metrics:Protocols=grpc+http2
Kestrel:Endpoints:orders:Url=http://127.0.0.1:4317/orders
Kestrel:Endpoints:orders:Protocols=grpc+http2
```

两个逻辑范围共用一个 HTTP/2 物理监听。转换工具保留原始完整 URL，输出 URL 只含 scheme、主机及端口；证书、SNI、默认连接选项保留。相同 socket 的连接选项必须一致，监听重叠、明文 HTTP 多版本混用及 SNI 扩大声明能力由转换工具拒绝。WebEndpointCollection 合并等效声明，并单独校验同监听、同类别的范围冲突。转换成功不代表请求范围没有歧义。

`WebEndpoint` 通过名称、NetworkAddress 和协议集合构造，复制并保留只读声明；Supports(httpVersion) 判断是否声明了指定 HTTP 主版本。WebEndpointCollection 按路径长度降序及名称序数排序，规范化协议并构建请求类别范围；模型及通用管线不引用 Kestrel 类型。转换器只使用显式协议或 EndpointDefaults:Protocols，不推断未声明能力。

纯转换对 grpc+http2、custom+http2 等显式承载不要求注册应用协议；裸名称需要传入规则才能补全。转换不检查业务处理器。启用请求范围时，上层协议必须有对应运行规则，不能从转换成功推断运行规则已注册。

Web 主库内置普通 HTTP 规则。上层协议必须在创建快照前通过 `AddWebEndpointProtocol` 注册；仅有通用协议语法并不表示已安装对应运行规则。未注册、重名、独立类别重复均报错；即时声明重载创建快照后不允许追加协议，工厂重载允许在最终容器构建前补充注册，不静默退回普通 HTTP。重复注册同一实例在快照创建前无副作用。`WebEndpointCollection` 的可选第二参数也可直接接收协议规则集合；它复制注册集合，规则实例应保持不可变或线程安全。

编码协议可共用普通 HTTP 类别。例如 `services.AddWebEndpointProtocol(new WebEndpointProtocol("protobuf"));` 表达 `protobuf → protobuf+http`；`protobuf,protobuf+http2` 归并为 `protobuf+http2`。这一声明不安装序列化器。运行端需要具体 HTTP 版本，仅有 `http` 或展开后的 `protobuf+http` 不足以配置监听；直接 TCP/UDP 组合不由此 Kestrel 适配器承载。

原生 gRPC 规则在独立包 `Zongsoft.Web.Grpc` 中。即时声明重载先调用该包的 `services.AddGrpcWebEndpoints()`；工厂重载在最终容器构建前完成协议注册即可。插件的 `GrpcInitializer.Registration` 已自动注册这项规则；Plugins.Web 通过显式端点扩展在最终服务提供器中建立快照。该规则将裸 `grpc` 展开为 `grpc+http2`，接受 HTTP/2、HTTP/3 承载，拒绝不适用的承载；Web 主库不包含 gRPC 名称判断。

`grpc+http3` 的 HTTPS 声明可生成 HTTP/3 投影，创建快照不探测后端或 QUIC 支持。实际启动仍受 Kestrel、TLS 和平台条件约束。HTTP/3 的运行验收与 TCP socket 绑定验证分别进行。

主机支持 IP、通配地址及 localhost；其它 DNS 名称不用于监听。* 明确为 IPv4，IPv6 的 TCP socket 在 Bind 前设置 DualMode=false；非管理 socket 使用原有工厂。此回调不改变 QUIC 的 UDP socket 行为。localhost 由 Kestrel展开为 IPv4/IPv6 回环。

## 路径与类别

声明基路径只能使用 ASCII 字母、数字和 -._~ 分段；拒绝中间空段、独立点段、转义、模板及通配符。基路径忽略大小写及可选尾斜杠，`/metrics`、`/Metrics/` 均匹配自身和路径段子路径；`/metrics-extra` 不匹配。WebEndpoint.Address 保留原始声明，Path 去除非根路径的尾斜杠。等效的同后端声明合并，独立后端由代理制作阶段检查冲突；同一逻辑区域可关联多个监听。

管线读取真实 Connection.LocalIpAddress/LocalPort、连接协议和 IHttpRequestFeature.RawTarget；不使用 Host 或转发头选择监听。原始 path 去掉 query，保留百分号编码和点段；absolute-form 提取原路径，OPTIONS * 不进入映射。无效请求目标返回 400。

先按 Content-Type 区分请求类别，再选择类别内最长路径。原生 gRPC 接受 application/grpc 与 application/grpc+格式，大小写不敏感并允许媒体类型参数；grpc-web 不属于该类别。HTTP/1 的 gRPC 请求返回 400，非 POST 返回 405，不回退到普通 HTTP。普通请求无范围返回 404；gRPC 无范围返回空的 trailers-only 响应，HTTP 200、grpc-status=12。

选定范围后不因版本不符、方法未知或路由失败改选较短范围。如果 ASP.NET 已解析路径落入另一范围或离开当前范围，则在普通处理器之前返回 404，不改派请求；同一范围内的业务参数仍可正常解码。请求体、查询、头、取消和原有路径均不由此管线改写。

## 端点约束

`WebEndpointFeature` 保存选定端点、原始路径及 `Kind`。`WebEndpointMetadata` 将 ASP.NET 路由端点关联到相同的完整基路径及请求类别：

```csharp
routes.MapGroup("/opentelemetry")
	.WithMetadata(new WebEndpointMetadata("/opentelemetry", "grpc"));
```

gRPC 扩展须在该组内注册服务及区域 fallback。普通未标注路由仅参与 HTTP 类别；标注路由与已选基路径忽略大小写及可选尾斜杠关联。gRPC 适配层仅对基路径之后的服务名、方法名检查精确大小写及编码。较短区域的 fallback 不能接管子区域未知方法。Web 主库不引入 gRPC 包；Web.Grpc 在这些路由组内实现 Handler 服务挂载、区域反射及区域 fallback，详见其[服务契约](../grpc/README.zh-Hans.md#基路径与区域)。

## 协议扩展与职责

`WebEndpointProtocol` 以 `Name`、`Carrier`、`Kind` 分别描述上层协议名称、默认承载及请求类别。普通编码协议直接实例化即可；独立类别派生类型使用受保护构造函数指定 `kind`，并覆盖实际需要的步骤：

| 步骤 | 契约 |
| --- | --- |
| Normalize | 补全或校验声明，保留上层名称；空协议表示拒绝 |
| Matches | 独立类别识别，在最长路径选择之前执行；多个类别同时命中报错，普通 HTTP 为默认类别 |
| Validate | 分类后、路径选择前校验请求；零表示通过，否则返回 HTTP 状态码 |
| Accepts | 通用路由范围验证之后的协议专属候选检查 |
| RejectAsync | 没有匹配范围时的响应，默认 404 |

gRPC 的媒体类型、方法限制、精确方法路径及 grpc-status 响应均由该包的私有策略实现。主库的中间件和匹配策略是 `WebEndpointExtension` 的私有嵌套类，不作为公开扩展点。

KestrelUtility 消费服务器配置；逻辑端点集合与通用请求管线只消费端点声明和协议规则。这些设施不解析 zongsoft.tools.binding PI、不生成外部入口，也不自动为控制器加前缀。普通 HTTP 同样可以声明 `/api` 与 `/admin` 的不同监听范围。部署工具另行消费 binding 暴露声明生成代理映射。

## 验证与交付

[转换测试](../test/Hosting/KestrelUtilityTest.cs)覆盖独立转换、简写规则、共享监听、原配置隔离及 IPv6 TCP 绑定；[逻辑端点测试](../test/Hosting/WebEndpointCollectionTest.cs)覆盖无配置构造、声明复制和范围冲突；[普通请求测试](../test/Hosting/WebEndpointPipelineTest.cs)在不引用 gRPC 扩展的项目中验证独立运行、类别与注册契约。[gRPC 专项测试](../grpc/test/GrpcEndpointPipelineTest.cs)覆盖媒体类型、原始路径、范围隔离和本机隔离 Kestrel 的 HTTP/1、HTTP/2 请求；[协议注册测试](../grpc/test/GrpcEndpointProtocolTest.cs)覆盖简写、插件注册及缺失扩展。带 gRPC 媒体类型的请求验证范围分派，不等同于完整 gRPC 调用验收。

Debug 使用本地 Core 输出，Release 的最低 Core 包依赖见项目文件。发布 Web 前须提供包含网络值 API 的对应 Core 构建产物；本地 Release 验证使用隔离包源，不代表该包已经推送。插件宿主接入见 [Plugins.Web](../../Zongsoft.Plugins.Web/README.zh-Hans.md)；Web.Grpc 在独立 HTTP/2 测试中验证区域服务、反射和流；Diagnostics 客户端、Nginx 和实际 HTTP/3 运行分别进行集成验收。

Kestrel 配置加载规则参见 [Microsoft 官方文档](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)；路由约束基于 [ASP.NET Core 路由扩展点](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)。


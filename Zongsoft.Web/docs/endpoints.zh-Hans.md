# Web 端点与请求范围

[English](endpoints.md) | [简体中文](endpoints.zh-Hans.md)

`Zongsoft.Web.Hosting` 为宿主提供启动端点快照、Kestrel 配置投影和请求范围约束。独立 ASP.NET 宿主必须在配置加载完成后显式接入；仅引用程序集或添加 binding 处理指令不会启动这套管线。Plugins.Web 的构建器已自动接入，不要重复注册快照或中间件；gRPC 服务注册由 Web.Grpc 完成。

## 宿主接入

下列片段放在 ASP.NET Core WebApplicationBuilder 的配置完成阶段。Kestrel 配置必须已有有效端点；示例中的 builder 是现有构建器。

```csharp
using Zongsoft.Web.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

var endpoints = builder.Services.AddWebEndpoints(
	builder.Configuration.GetSection("Kestrel"));

builder.WebHost.ConfigureKestrel(endpoints.Configure);
builder.Services.Configure<SocketTransportOptions>(endpoints.Configure);

var app = builder.Build();
app.UseWebEndpoints();
app.UseRouting();
// 身份验证、授权、静态文件及现有端点映射在此之后接入。
```

`AddWebEndpoints` 返回注册的 `WebEndpointCollection`，供宿主完成最终 Kestrel 配置。应在其它 Kestrel 配置回调之后安装投影，在原配置端点加载之前完成；不得先加载含路径的原 URL，也不应另用 Listen、urls 或环境变量创建未登记监听。网络配置变更时，宿主通过 `HasChanged(kestrelSection)` 判断并报告需要重启，不重新配置监听或路由。

`UseWebEndpoints` 必须位于路由、转发头及可能提前返回的普通 HTTP 中间件之前，不可重复安装。它不代替身份验证和授权。范围约束只管监听及路由归属；任意自定义中间件仍由应用负责。静态文件及普通端点不能通过 gRPC 专用范围接收普通请求。

## 快照与投影

每个配置项提供完整 `Url` 和 `Protocols`，例如：

```ini
Kestrel:Endpoints:metrics:Url=http://127.0.0.1:4317/opentelemetry
Kestrel:Endpoints:metrics:Protocols=grpc+http2
Kestrel:Endpoints:orders:Url=http://127.0.0.1:4317/orders
Kestrel:Endpoints:orders:Protocols=grpc+http2
```

两个逻辑范围共用一个 HTTP/2 物理监听。原配置保留完整 URL；投影 URL 只含 scheme、主机及端口。证书、SNI、默认连接选项保留；相同 socket 的各项连接选项必须一致。监听重叠、同类别同范围冲突、同类别仅大小写不同的路径及明文 HTTP 多版本混用均失败。SNI 原生 Protocols 不能扩大端点声明的版本集合。

`WebEndpoint` 公开配置名称、`NetworkAddress`、只读 `NetworkProtocol` 集合及所有类别的 HTTP 承载版本 `HttpProtocols`；`GetProtocols(kind)` 返回指定请求类别的版本，默认类别为 `http`。集合按路径长度降序及名称序数排序。未配置项不会从服务器默认值推导能力；显式的 `EndpointDefaults:Protocols` 可作为端点配置默认值。

Web 主库内置普通 HTTP 规则。上层协议必须在创建快照前通过 `AddWebEndpointProtocol` 注册；仅有通用协议语法并不表示已安装对应运行规则。未注册、重名、独立类别重复或快照创建后追加注册均报错，不静默退回普通 HTTP。重复注册同一实例在快照创建前无副作用。`WebEndpointCollection` 的可选第二参数也可直接接收协议规则集合；它复制注册集合，规则实例应保持不可变或线程安全。

编码协议可共用普通 HTTP 类别。例如 `services.AddWebEndpointProtocol(new WebEndpointProtocol("protobuf"));` 表达 `protobuf → protobuf+http`；`protobuf,protobuf+http2` 归并为 `protobuf+http2`。这一声明不安装序列化器。运行端需要具体 HTTP 版本，仅有 `http` 或展开后的 `protobuf+http` 不足以配置监听；直接 TCP/UDP 组合不由此 Kestrel 适配器承载。

原生 gRPC 规则在独立包 `Zongsoft.Web.Grpc` 中。显式宿主先调用该包的 `services.AddGrpcWebEndpoints()`，再调用 `AddWebEndpoints`。插件的 `GrpcInitializer.Registration` 已自动注册这项规则；Plugins.Web 在全部插件注册完成后自动建立快照。该规则将裸 `grpc` 展开为 `grpc+http2`，接受 HTTP/2、HTTP/3 承载，拒绝不适用的承载；Web 主库不包含 gRPC 名称判断。

`grpc+http3` 的 HTTPS 声明可生成 HTTP/3 投影，创建快照不探测后端或 QUIC 支持。实际启动仍受 Kestrel、TLS 和平台条件约束。HTTP/3 的运行验收与 TCP socket 绑定验证分别进行。

主机支持 IP、通配地址及 localhost；其它 DNS 名称不用于监听。* 明确为 IPv4，IPv6 的 TCP socket 在 Bind 前设置 DualMode=false；非管理 socket 使用原有工厂。此回调不改变 QUIC 的 UDP socket 行为。localhost 由 Kestrel展开为 IPv4/IPv6 回环。

## 路径与类别

声明基路径只能使用 ASCII 字母、数字和 -._~ 分段；拒绝中间空段、独立点段、转义、模板及通配符。保留大小写和尾斜杠。`/metrics` 匹配自身及子路径；`/metrics/` 不匹配无尾斜杠的自身。

管线读取真实 Connection.LocalIpAddress/LocalPort、连接协议和 IHttpRequestFeature.RawTarget；不使用 Host 或转发头选择监听。原始 path 去掉 query，保留百分号编码和点段；absolute-form 提取原路径，OPTIONS * 不进入映射。无效请求目标返回 400。

先按 Content-Type 区分请求类别，再选择类别内最长路径。原生 gRPC 接受 application/grpc 与 application/grpc+格式，大小写不敏感并允许媒体类型参数；grpc-web 不属于该类别。HTTP/1 的 gRPC 请求返回 400，非 POST 返回 405，不回退到普通 HTTP。普通请求无范围返回 404；gRPC 无范围返回空的 trailers-only 响应，HTTP 200、grpc-status=12。

选定范围后不因版本不符、方法未知或路由失败改选较短范围。如果 ASP.NET 已解析路径落入另一范围或离开当前范围，则在普通处理器之前返回 404，不改派请求；同一范围内的业务参数仍可正常解码。请求体、查询、头、取消和原有路径均不由此管线改写。

## 端点约束

`WebEndpointFeature` 保存选定端点、原始路径及 `Kind`。`WebEndpointMetadata` 将 ASP.NET 路由端点关联到相同的完整基路径及请求类别：

```csharp
routes.MapGroup("/opentelemetry")
	.WithMetadata(new WebEndpointMetadata("/opentelemetry", "grpc"));
```

gRPC 扩展须在该组内注册服务及区域 fallback。普通未标注路由仅参与 HTTP 类别；标注路由必须与已选范围精确对应。gRPC 固定方法路由额外比较原始完整路径的大小写及编码。较短区域的 fallback 不能接管子区域未知方法。Web 主库不引入 gRPC 包；Handler 挂载、区域反射及区域 fallback 尚待 Web.Grpc 的后续服务挂载步骤完成。

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

这些设施消费 Kestrel 运行配置，不解析 binding PI、不生成外部入口，也不自动为控制器加前缀。它们将含路径的声明转换为物理监听，并在请求到达时兑现逻辑范围。普通 HTTP 同样可以声明 `/api` 与 `/admin` 的不同监听范围。部署工具另行消费 binding 暴露声明生成代理映射。

## 验证与交付

[快照与投影测试](../test/Hosting/WebEndpointCollectionTest.cs)覆盖通用协议、共享监听、配置冲突、快照独立性及 IPv6 TCP 绑定；[普通请求测试](../test/Hosting/WebEndpointPipelineTest.cs)在不引用 gRPC 扩展的项目中验证独立运行、类别与注册契约。[gRPC 专项测试](../grpc/test/GrpcEndpointPipelineTest.cs)覆盖媒体类型、原始路径、范围隔离和本机隔离 Kestrel 的 HTTP/1、HTTP/2 请求；[协议注册测试](../grpc/test/GrpcEndpointProtocolTest.cs)覆盖简写、插件注册及缺失扩展。带 gRPC 媒体类型的请求验证范围分派，不等同于完整 gRPC 调用验收。

Debug 使用本地 Core 输出，Release 的最低 Core 包依赖见项目文件。发布 Web 前须提供包含网络值 API 的对应 Core 构建产物；本地 Release 验证使用隔离包源，不代表该包已经推送。插件宿主接入见 [Plugins.Web](../../Zongsoft.Plugins.Web/README.zh-Hans.md)；完整 gRPC 服务/反射/流、Nginx 和实际 HTTP/3 运行属于后续集成验收。

Kestrel 配置加载规则参见 [Microsoft 官方文档](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints)；路由约束基于 [ASP.NET Core 路由扩展点](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing)。


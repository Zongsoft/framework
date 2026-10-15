---
name: zongsoft-web
description: 开发、审查、调试或测试 Zongsoft.Web 及其 OpenAPI、gRPC 扩展。适用于服务控制器、模型绑定、过滤器、格式化、路由、HTTP、安全管线、SignalR、OpenAPI 文档和 gRPC 服务接入；不用于业务站点控制器或插件宿主装配。
---

# Zongsoft.Web 通用 Web 能力

## 定位

1. 阅读 [AGENTS.md](AGENTS.md) 和目标功能附近的测试。
2. 确认行为属于 ASP.NET 通用管线、服务控制器约定、OpenAPI 扩展或 gRPC 扩展。
3. 若问题是插件宿主/控制器发现，转到 [../Zongsoft.Plugins/SKILL.md](../Zongsoft.Plugins/SKILL.md)；若是身份权限模型，检查 `Zongsoft.Security`。

## 源码与跨项目同步点

- [ServiceControllerBase](src/ServiceControllerBase.cs) 与 [ServiceController](src/ServiceController.cs) 把 HTTP 操作委托给数据服务；最小派生类使用真实的无参构造/GetService 扩展点，不编造 service 基类构造参数。
- [ControllerActivator](src/ControllerActivator.cs) 与 [ControllerFeatureProvider](src/ControllerFeatureProvider.cs) 负责激活/发现边界；插件程序集由 Plugins.Web 的 WebApplicationContext 加入 ApplicationParts，单独包引用不能证明路由存在。
- [Binders](src/Binders) 解析条件/排序/范围等文本，[Formatters](src/Formatters) 负责 JSON，[Filters](src/Filters) 负责横切处理。HTTP Schema 与排序格式也受 Core/Data 契约约束。
- [Plugins.Web Application](../Zongsoft.Plugins.Web/src/Application.cs) 调用初始化器后依次加入 CORS、本地化、方法覆盖、路由、认证、授权、压缩与静态文件，最后映射 Controller/Hub。不能在可选扩展中无意重复注册整条管线。
- [GrpcInitializer](grpc/src/GrpcInitializer.cs) 注册 gRPC 并按服务标签映射类型；只继承生成基类而未满足注册/标签约定时，不会自动出现端点。
- [Plugins.Web 默认策略](../Zongsoft.Plugins.Web/src/WebApplicationBuilder.cs) 包含宽松 CORS。安全部署必须校准宿主策略，不把默认认证中间件存在等同于所有接口已经授权保护。

## 契约

- 保持路由、参数来源、模型绑定、分页、HTTP 状态码、错误负载和序列化形状兼容。
- ServiceController 的 CRUD 与子服务行为应复用 Data/Services 契约，不复制领域验证。
- Filter、Formatter 和 Binder 必须正确处理空输入、取消、不可读 Body、重复读取和响应已开始。
- 安全组件只从 ASP.NET 上下文桥接身份与授权，不在 Web 层重新定义安全模型。
- OpenAPI 和 gRPC 为可选包；主库不得引入其依赖。配置或插件变化同步 `.plugin`、`.option`、`.deploy`。
- `.deploy` 不引用 Zongsoft.Core、Zongsoft.Web、Zongsoft.Plugins、Zongsoft.Plugins.Web；这四个基础库由宿主统一提供，编译依赖保留在项目文件中。

## 启动端点与请求范围

[Hosting 端点设施](docs/endpoints.zh-Hans.md) 从最终 Kestrel 配置创建 WebEndpointCollection，保留逻辑 URL、应用协议及路径，独立投影物理监听，禁止网络热重载。宿主在配置加载完成后调用 AddWebEndpoints，再在最终 Kestrel 回调及 SocketTransportOptions 中使用 Configure；在路由、转发头和静态文件之前安装 UseWebEndpoints。HasChanged 只比较配置与启动快照，重启提示和宿主生命周期接入由 Plugins.Web 负责。Plugins.Web 在最终插件注册后自动接入，使用最终 PostConfigure 应用 Kestrel 投影，并在构建后、应用初始化器前安装范围中间件。

Core 只解析通用网络值；HTTP 基路径限制、Kestrel 承载转换及类别内的范围选择属于 Web。上层协议通过 WebEndpointProtocol 注册，默认普通 HTTP；编码协议可注册默认承载并共用 http 类别。gRPC 简写、媒体类型、方法/版本校验、精确方法路径及无范围响应位于 Web.Grpc 的私有协议策略，由 AddGrpcWebEndpoints 和 GrpcInitializer.Registration 注册。不得在主库重加 grpc/protobuf 名称判断、IsGrpc 或 GrpcProtocols。注册必须先于快照；缺失、重名、类别识别歧义及晚注册失败，不按注册顺序猜归属。

按真实本地连接、请求类别和原始 URI 路径选择范围，不改写源配置或请求。已解析路径不得进入另一逻辑范围；这是拒绝校验，不改派后端。WebEndpointFeature 固定选择结果及 Kind，WebEndpointMetadata 关联范围与类别。中间件和 MatcherPolicy 为 WebEndpointExtension 的私有嵌套类，测试通过公开入口执行。Handler 服务挂载、区域 fallback、反射留待 Web.Grpc 的对应步骤；主库不引入 gRPC 依赖。HTTP/3 仅声明及配置投影通过不代表 QUIC 运行验收通过。

主库测试为 test/Hosting 的 WebEndpointCollectionTest 与 WebEndpointPipelineTest，不引用 gRPC；协议专项在 grpc/test 的独立项目中，通过公开注册和真实 ASP.NET 管线验证，不开放内部方法。grpc 与 openapi 均以 src 存放主项目和插件构件，test 存放测试；README 保留在各扩展根目录。OpenAPI 暂无独立测试项目，test 目录保留说明。Release 使用项目指定的 Core/Web 包，必须包含本次契约；验证使用隔离本地包源，不能把 Debug 本地引用成功当作旧 NuGet 包可用。

## 验证

构建 `Zongsoft.Web.slnx` 并运行相关测试。HTTP 行为使用最小 TestServer/WebApplicationFactory 或现有夹具验证成功、无效输入、未认证、取消和异常映射；OpenAPI/gRPC 分别构建对应项目。
